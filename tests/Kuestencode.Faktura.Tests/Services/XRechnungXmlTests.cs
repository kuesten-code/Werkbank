using System.Xml.Linq;
using FluentAssertions;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

/// <summary>
/// Prüft den Inhalt der erzeugten XRechnung (CII) — insbesondere Steuerkategorie, Steuersatz und Summen
/// in den drei Steuermodi (Regelbesteuerung, §19 UStG, §13b UStG).
/// </summary>
public class XRechnungXmlTests
{
    private static readonly XNamespace Ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
    private static readonly XNamespace Rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";

    private readonly Mock<IInvoiceService> _invoiceService = new();
    private readonly Mock<ICompanyService> _companyService = new();
    private readonly Mock<IPdfGeneratorService> _pdfGeneratorService = new();
    private readonly XRechnungService _service;
    private Company _company = MakeCompany();

    public XRechnungXmlTests()
    {
        _companyService.Setup(c => c.GetCompanyAsync()).ReturnsAsync(() => _company);
        _service = new XRechnungService(
            _invoiceService.Object, _companyService.Object, _pdfGeneratorService.Object,
            NullLogger<XRechnungService>.Instance);
    }

    private static Company MakeCompany() => new()
    {
        OwnerFullName = "Erika Musterfrau",
        Address = "Hafenstraße 1",
        PostalCode = "20457",
        City = "Hamburg",
        Country = "Deutschland",
        TaxNumber = "12/345/67890",
        IsKleinunternehmer = false,
        BankAccount = "DE89370400440532013000",
        BankName = "Sparkasse",
        Email = "info@kuestencode.de"
    };

    private static Invoice MakeInvoice(params InvoiceItem[] items) => new()
    {
        Id = 1,
        InvoiceNumber = "R-2026-0001",
        InvoiceDate = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
        CustomerId = 1,
        Customer = new Customer
        {
            Name = "Nordlicht Media",
            Address = "Speicherstadt 5",
            PostalCode = "20457",
            City = "Hamburg",
            Country = "Deutschland"
        },
        Items = items.Length > 0
            ? items.ToList()
            : [new InvoiceItem { Description = "Beratung", Quantity = 2, UnitPrice = 100m, VatRate = 19 }]
    };

    private async Task<XDocument> GenerateAsync(Invoice invoice)
    {
        _invoiceService.Setup(s => s.GetByIdAsync(invoice.Id, true, true)).ReturnsAsync(invoice);
        return XDocument.Parse(await _service.GenerateXRechnungXmlAsync(invoice.Id));
    }

    private static XElement HeaderTax(XDocument doc) =>
        doc.Descendants(Ram + "ApplicableHeaderTradeSettlement").Single().Element(Ram + "ApplicableTradeTax")!;

    private static XElement Summation(XDocument doc) =>
        doc.Descendants(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation").Single();

    private static string? IncludedNote(XDocument doc) =>
        doc.Descendants(Ram + "IncludedNote").SingleOrDefault()?.Element(Ram + "Content")?.Value;

    // ─── Steuermodi ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Regelbesteuerung_Kategorie_S_Mit19ProzentUndOhneHinweis()
    {
        var doc = await GenerateAsync(MakeInvoice());

        var tax = HeaderTax(doc);
        tax.Element(Ram + "CategoryCode")!.Value.Should().Be("S");
        tax.Element(Ram + "RateApplicablePercent")!.Value.Should().Be("19.00");
        tax.Element(Ram + "ExemptionReasonCode").Should().BeNull();
        tax.Element(Ram + "BasisAmount")!.Value.Should().Be("200.00");
        tax.Element(Ram + "CalculatedAmount")!.Value.Should().Be("38.00");
        IncludedNote(doc).Should().BeNull();

        var lineTax = doc.Descendants(Ram + "SpecifiedLineTradeSettlement").Single().Element(Ram + "ApplicableTradeTax")!;
        lineTax.Element(Ram + "CategoryCode")!.Value.Should().Be("S");
        lineTax.Element(Ram + "RateApplicablePercent")!.Value.Should().Be("19.00");
    }

    [Fact]
    public async Task Kleinunternehmer_Kategorie_E_NullProzentUndParagraph19Hinweis()
    {
        _company.IsKleinunternehmer = true;
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "Beratung", Quantity = 2, UnitPrice = 100m, VatRate = 0 }));

        var tax = HeaderTax(doc);
        tax.Element(Ram + "CategoryCode")!.Value.Should().Be("E");
        tax.Element(Ram + "RateApplicablePercent")!.Value.Should().Be("0.00");
        tax.Element(Ram + "ExemptionReasonCode")!.Value.Should().Be("VATEX-EU-O");
        tax.Element(Ram + "CalculatedAmount")!.Value.Should().Be("0.00");
        IncludedNote(doc).Should().Contain("§ 19 UStG");
        Summation(doc).Element(Ram + "GrandTotalAmount")!.Value.Should().Be("200.00");
    }

    [Fact]
    public async Task ReverseCharge_Kategorie_AE_NullProzentUndParagraph13bHinweis()
    {
        var invoice = MakeInvoice(new InvoiceItem { Description = "Montage", Quantity = 1, UnitPrice = 500m, VatRate = 0 });
        invoice.IsReverseCharge = true;

        var doc = await GenerateAsync(invoice);

        var tax = HeaderTax(doc);
        tax.Element(Ram + "CategoryCode")!.Value.Should().Be("AE");
        tax.Element(Ram + "RateApplicablePercent")!.Value.Should().Be("0.00");
        tax.Element(Ram + "ExemptionReasonCode")!.Value.Should().Be("VATEX-EU-AE");
        IncludedNote(doc).Should().Contain("§ 13b UStG");
        doc.Descendants(Ram + "SpecifiedLineTradeSettlement").Single()
            .Descendants(Ram + "CategoryCode").Single().Value.Should().Be("AE");
    }

    [Fact]
    public async Task KleinunternehmerHatVorrangVorReverseCharge()
    {
        _company.IsKleinunternehmer = true;
        var invoice = MakeInvoice(new InvoiceItem { Description = "Montage", Quantity = 1, UnitPrice = 500m, VatRate = 0 });
        invoice.IsReverseCharge = true;

        var doc = await GenerateAsync(invoice);

        HeaderTax(doc).Element(Ram + "CategoryCode")!.Value.Should().Be("E");
        IncludedNote(doc).Should().Contain("§ 19 UStG");
    }

    // ─── Summen & Rundung ─────────────────────────────────────────────────────

    [Fact]
    public async Task Summen_PositionenKaufmaennischGerundet_HeaderSummiertGerundetePositionen()
    {
        // MwSt je Position gerundet: 99,99 × 19 % = 18,9981 → 19,00; 15,00 × 19 % = 2,85
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "A", Quantity = 3, UnitPrice = 33.33m, VatRate = 19 },
            new InvoiceItem { Description = "B", Quantity = 1.5m, UnitPrice = 10m, VatRate = 19 }));

        var sum = Summation(doc);
        sum.Element(Ram + "LineTotalAmount")!.Value.Should().Be("114.99");
        sum.Element(Ram + "TaxBasisTotalAmount")!.Value.Should().Be("114.99");
        sum.Element(Ram + "TaxTotalAmount")!.Value.Should().Be("21.85");
        sum.Element(Ram + "GrandTotalAmount")!.Value.Should().Be("136.84");
        sum.Element(Ram + "DuePayableAmount")!.Value.Should().Be("136.84");

        var lineTotals = doc.Descendants(Ram + "SpecifiedTradeSettlementLineMonetarySummation")
            .Select(e => e.Element(Ram + "LineTotalAmount")!.Value);
        lineTotals.Should().Equal("99.99", "15.00");
    }

    [Fact]
    public async Task Positionen_OhneBeschreibungWerdenUebersprungen_LineIdsFortlaufend()
    {
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "A", Quantity = 1, UnitPrice = 10m, VatRate = 19 },
            new InvoiceItem { Description = " ", Quantity = 1, UnitPrice = 0m, VatRate = 19 },
            new InvoiceItem { Description = "C", Quantity = 1, UnitPrice = 20m, VatRate = 19 }));

        doc.Descendants(Ram + "LineID").Select(e => e.Value).Should().Equal("1", "2");
        doc.Descendants(Ram + "BilledQuantity").First().Value.Should().Be("1.000");
    }

    // ─── Gutschrift ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Gutschrift_TypeCode381UndBetraegePositiv()
    {
        var invoice = MakeInvoice(new InvoiceItem { Description = "Beratung", Quantity = 2, UnitPrice = -100m, VatRate = 19 });
        invoice.Type = InvoiceType.CreditNote;

        var doc = await GenerateAsync(invoice);

        doc.Descendants(Ram + "TypeCode").First().Value.Should().Be("381");
        doc.Descendants(Ram + "ChargeAmount").Single().Value.Should().Be("100.00");
        Summation(doc).Element(Ram + "GrandTotalAmount")!.Value.Should().Be("238.00");
    }

    [Fact]
    public async Task Rechnung_TypeCode380()
    {
        var doc = await GenerateAsync(MakeInvoice());

        doc.Root!.Element(Rsm + "ExchangedDocument")!.Element(Ram + "TypeCode")!.Value.Should().Be("380");
        doc.Descendants(Ram + "IssueDateTime").Single().Value.Should().Be("20260315");
    }

    // ─── Verkäufer / Käufer ───────────────────────────────────────────────────

    [Fact]
    public async Task Verkaeufer_OhneUstIdUndEndpoint_SteuernummerAlsIdUndEmailAlsElektronischeAdresse()
    {
        var doc = await GenerateAsync(MakeInvoice());

        var seller = doc.Descendants(Ram + "SellerTradeParty").Single();
        seller.Element(Ram + "ID")!.Value.Should().Be("12/345/67890");
        seller.Element(Ram + "GlobalID").Should().BeNull();
        var uri = seller.Element(Ram + "URIUniversalCommunication")!.Element(Ram + "URIID")!;
        uri.Attribute("schemeID")!.Value.Should().Be("EM");
        uri.Value.Should().Be("info@kuestencode.de");
        seller.Elements(Ram + "SpecifiedTaxRegistration").Select(e => e.Element(Ram + "ID")!.Attribute("schemeID")!.Value)
            .Should().Equal("FC");
    }

    [Fact]
    public async Task Verkaeufer_MitEndpointUndUstId_GlobalIdUndBeideSteuerregistrierungen()
    {
        _company.EndpointId = " 9930:DE123456789 ";
        _company.EndpointSchemeId = "0204";
        _company.VatId = "DE123456789";
        _company.Phone = "040 123456";

        var doc = await GenerateAsync(MakeInvoice());

        var seller = doc.Descendants(Ram + "SellerTradeParty").Single();
        var globalId = seller.Element(Ram + "GlobalID")!;
        globalId.Attribute("schemeID")!.Value.Should().Be("0204");
        globalId.Value.Should().Be("9930:DE123456789");
        seller.Element(Ram + "ID").Should().BeNull();
        seller.Element(Ram + "URIUniversalCommunication")!.Element(Ram + "URIID")!.Value.Should().Be("9930:DE123456789");
        seller.Descendants(Ram + "CompleteNumber").Single().Value.Should().Be("040 123456");
        seller.Elements(Ram + "SpecifiedTaxRegistration").Select(e => e.Element(Ram + "ID")!.Attribute("schemeID")!.Value)
            .Should().Equal("FC", "VA");
    }

    [Fact]
    public async Task Verkaeufer_NurUstId_KeinSteuernummerFallback()
    {
        _company.TaxNumber = "";
        _company.VatId = "DE123456789";

        var doc = await GenerateAsync(MakeInvoice());

        var seller = doc.Descendants(Ram + "SellerTradeParty").Single();
        seller.Element(Ram + "ID").Should().BeNull();
        seller.Elements(Ram + "SpecifiedTaxRegistration").Select(e => e.Element(Ram + "ID")!.Attribute("schemeID")!.Value)
            .Should().Equal("VA");
    }

    [Fact]
    public async Task Kontoinhaber_GesetztWirdAlsAccountNameVerwendet_SonstInhaber()
    {
        var withoutHolder = await GenerateAsync(MakeInvoice());
        withoutHolder.Descendants(Ram + "AccountName").Single().Value.Should().Be("Erika Musterfrau");

        _company.AccountHolder = "Küstencode GbR";
        var withHolder = await GenerateAsync(MakeInvoice());
        withHolder.Descendants(Ram + "AccountName").Single().Value.Should().Be("Küstencode GbR");
    }

    [Fact]
    public async Task Kaeufer_MitEmail_ElektronischeAdresseMitSchemeEM()
    {
        var invoice = MakeInvoice();
        invoice.Customer!.Email = " kunde@nordlicht.de ";

        var doc = await GenerateAsync(invoice);

        var uri = doc.Descendants(Ram + "BuyerTradeParty").Single().Descendants(Ram + "URIID").Single();
        uri.Attribute("schemeID")!.Value.Should().Be("EM");
        uri.Value.Should().Be("kunde@nordlicht.de");
    }

    [Theory]
    [InlineData("Deutschland", "DE")]
    [InlineData("germany", "DE")]
    [InlineData("Österreich", "AT")]
    [InlineData("Austria", "AT")]
    [InlineData("Schweiz", "CH")]
    [InlineData("switzerland", "CH")]
    [InlineData("AT", "AT")]
    [InlineData("nl", "NL")]
    public async Task Laendercode_WirdNachIsoAlpha2Uebersetzt(string country, string expected)
    {
        var invoice = MakeInvoice();
        invoice.Customer!.Country = country;

        var doc = await GenerateAsync(invoice);

        doc.Descendants(Ram + "BuyerTradeParty").Single().Descendants(Ram + "CountryID").Single().Value.Should().Be(expected);
    }

    // ─── Zahlungsbedingungen ──────────────────────────────────────────────────

    [Fact]
    public async Task Faelligkeit_GesetztErzeugtZahlungsbedingung_SonstNicht()
    {
        var without = await GenerateAsync(MakeInvoice());
        without.Descendants(Ram + "SpecifiedTradePaymentTerms").Should().BeEmpty();

        var invoice = MakeInvoice();
        invoice.DueDate = new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc);
        var with = await GenerateAsync(invoice);
        with.Descendants(Ram + "SpecifiedTradePaymentTerms").Single().Value.Should().Be("20260414");
    }

    // ─── Fehlerfälle ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateXml_PflichtfeldFehlt_WirftMitAufzaehlungDerFelder()
    {
        _company.BankName = "";
        _invoiceService.Setup(s => s.GetByIdAsync(1, true, true)).ReturnsAsync(MakeInvoice());

        var act = () => _service.GenerateXRechnungXmlAsync(1);

        (await act.Should().ThrowAsync<InvalidOperationException>()).WithMessage("*Bankname*");
    }

    [Fact]
    public async Task GenerateXml_RechnungNichtGefunden_WirftArgumentException()
    {
        _invoiceService.Setup(s => s.GetByIdAsync(42, true, true)).ReturnsAsync((Invoice?)null);

        var act = () => _service.GenerateXRechnungXmlAsync(42);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Validate_CompanyServiceWirft_LiefertUngueltigMitFehlermeldung()
    {
        _invoiceService.Setup(s => s.GetByIdAsync(1, true, true)).ReturnsAsync(MakeInvoice());
        _companyService.Setup(c => c.GetCompanyAsync()).ThrowsAsync(new InvalidOperationException("Host nicht erreichbar"));

        var (isValid, missing) = await _service.ValidateForXRechnungAsync(1);

        isValid.Should().BeFalse();
        missing.Should().ContainSingle(m => m.Contains("Host nicht erreichbar"));
    }

    // ─── ZUGFeRD ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ZugferdPdf_BettetFacturXXmlAlsAssociatedFileEin()
    {
        var invoice = MakeInvoice();
        _invoiceService.Setup(s => s.GetByIdAsync(1, true, true)).ReturnsAsync(invoice);
        _pdfGeneratorService.Setup(p => p.GenerateInvoicePdfAsync(1)).ReturnsAsync(CreateMinimalPdf());

        var bytes = await _service.GenerateZugferdPdfAsync(1);

        using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
        var catalog = pdf.GetCatalog().GetPdfObject();
        catalog.GetAsArray(PdfName.AF).Size().Should().Be(1);
        var names = catalog.GetAsDictionary(PdfName.Names).GetAsDictionary(PdfName.EmbeddedFiles).GetAsArray(PdfName.Names);
        names.GetAsString(0).ToUnicodeString().Should().Be("factur-x.xml");
    }

    [Fact]
    public async Task ZugferdPdf_PdfErzeugungSchlaegtFehl_ExceptionWirdWeitergereicht()
    {
        _pdfGeneratorService.Setup(p => p.GenerateInvoicePdfAsync(1)).ThrowsAsync(new IOException("kaputt"));

        var act = () => _service.GenerateZugferdPdfAsync(1);

        await act.Should().ThrowAsync<IOException>();
    }

    private static byte[] CreateMinimalPdf()
    {
        using var stream = new MemoryStream();
        using (var document = new Document(new PdfDocument(new PdfWriter(stream))))
        {
            document.Add(new Paragraph("Rechnung"));
        }
        return stream.ToArray();
    }

    // ─── Steuersätze, Rabatt, Abschläge, negative Positionen ──────────────────

    private static IEnumerable<XElement> HeaderTaxes(XDocument doc) =>
        doc.Descendants(Ram + "ApplicableHeaderTradeSettlement").Single().Elements(Ram + "ApplicableTradeTax");

    private static decimal Amount(XElement element) =>
        decimal.Parse(element.Value, System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public async Task Ermaessigt7Prozent_HeaderSteuersatz7()
    {
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "Fachbuch", Quantity = 1, UnitPrice = 100m, VatRate = 7 }));

        var tax = HeaderTaxes(doc).Single();
        tax.Element(Ram + "RateApplicablePercent")!.Value.Should().Be("7.00");
        tax.Element(Ram + "CalculatedAmount")!.Value.Should().Be("7.00");
        tax.Element(Ram + "CategoryCode")!.Value.Should().Be("S");
    }

    [Fact]
    public async Task GemischteSteuersaetze_EinSteuerblockJeSatzUndSummenGehenAuf()
    {
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "Fachbuch", Quantity = 1, UnitPrice = 100m, VatRate = 7 },
            new InvoiceItem { Description = "Beratung", Quantity = 2, UnitPrice = 100m, VatRate = 19 }));

        HeaderTaxes(doc).Select(t => (t.Element(Ram + "RateApplicablePercent")!.Value, t.Element(Ram + "BasisAmount")!.Value, t.Element(Ram + "CalculatedAmount")!.Value))
            .Should().Equal(("19.00", "200.00", "38.00"), ("7.00", "100.00", "7.00"));
        var sum = Summation(doc);
        sum.Element(Ram + "TaxTotalAmount")!.Value.Should().Be("45.00");
        sum.Element(Ram + "GrandTotalAmount")!.Value.Should().Be("345.00");
    }

    [Fact]
    public async Task RegelbesteuerungMitNullProzentPosition_KategorieZ()
    {
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "Porto", Quantity = 1, UnitPrice = 5m, VatRate = 0 }));

        HeaderTaxes(doc).Single().Element(Ram + "CategoryCode")!.Value.Should().Be("Z");
        doc.Descendants(Ram + "SpecifiedLineTradeSettlement").Single()
            .Descendants(Ram + "CategoryCode").Single().Value.Should().Be("Z");
    }

    [Fact]
    public async Task Rabatt_AlsNachlassJeSteuersatzUndGesamtbetragWieRechnung()
    {
        var invoice = MakeInvoice(
            new InvoiceItem { Description = "Fachbuch", Quantity = 1, UnitPrice = 100m, VatRate = 7 },
            new InvoiceItem { Description = "Beratung", Quantity = 1, UnitPrice = 100m, VatRate = 19 });
        invoice.DiscountType = DiscountType.Percentage;
        invoice.DiscountValue = 10;

        var doc = await GenerateAsync(invoice);

        var allowances = doc.Descendants(Ram + "SpecifiedTradeAllowanceCharge").ToList();
        allowances.Should().HaveCount(2);
        allowances.Should().AllSatisfy(a =>
        {
            a.Element(Ram + "ChargeIndicator")!.Value.Should().Be("false");
            a.Element(Ram + "ActualAmount")!.Value.Should().Be("10.00");
            a.Element(Ram + "Reason")!.Value.Should().Be("Rabatt");
        });
        allowances.Select(a => a.Descendants(Ram + "RateApplicablePercent").Single().Value).Should().Equal("19.00", "7.00");

        var sum = Summation(doc);
        sum.Element(Ram + "LineTotalAmount")!.Value.Should().Be("200.00");
        sum.Element(Ram + "AllowanceTotalAmount")!.Value.Should().Be("20.00");
        sum.Element(Ram + "TaxBasisTotalAmount")!.Value.Should().Be("180.00");
        sum.Element(Ram + "TaxTotalAmount")!.Value.Should().Be("23.40");
        Amount(sum.Element(Ram + "GrandTotalAmount")!).Should().Be(Math.Round(invoice.TotalGross, 2));
    }

    [Fact]
    public async Task OhneRabatt_KeinNachlassElement()
    {
        var doc = await GenerateAsync(MakeInvoice());

        doc.Descendants(Ram + "SpecifiedTradeAllowanceCharge").Should().BeEmpty();
        Summation(doc).Element(Ram + "AllowanceTotalAmount")!.Value.Should().Be("0.00");
    }

    [Fact]
    public async Task Abschlag_AlsBereitsGezahltUndZahlbetragWieZuZahlen()
    {
        var invoice = MakeInvoice(new InvoiceItem { Description = "Leistung", Quantity = 1, UnitPrice = 1000m, VatRate = 19 });
        invoice.DownPayments.Add(new DownPayment { Description = "AR-1", Amount = 595m });

        var doc = await GenerateAsync(invoice);

        var sum = Summation(doc);
        sum.Element(Ram + "GrandTotalAmount")!.Value.Should().Be("1190.00");
        sum.Element(Ram + "TotalPrepaidAmount")!.Value.Should().Be("595.00");
        sum.Element(Ram + "DuePayableAmount")!.Value.Should().Be("595.00");
    }

    [Fact]
    public async Task OhneAbschlag_KeinTotalPrepaidAmount()
    {
        var doc = await GenerateAsync(MakeInvoice());

        Summation(doc).Element(Ram + "TotalPrepaidAmount").Should().BeNull();
    }

    [Fact]
    public async Task NegativePosition_PositiverPreisNegativeMengeUndZeilensummeGleichKopf()
    {
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "Beratung", Quantity = 1, UnitPrice = 100m, VatRate = 19 },
            new InvoiceItem { Description = "Nachlass", Quantity = 1, UnitPrice = -20m, VatRate = 19 }));

        var discountLine = doc.Descendants(Ram + "IncludedSupplyChainTradeLineItem").Last();
        discountLine.Descendants(Ram + "ChargeAmount").Single().Value.Should().Be("20.00");
        discountLine.Descendants(Ram + "BilledQuantity").Single().Value.Should().Be("-1.000");
        discountLine.Descendants(Ram + "LineTotalAmount").Single().Value.Should().Be("-20.00");

        var lineSum = doc.Descendants(Ram + "SpecifiedTradeSettlementLineMonetarySummation").Sum(e => Amount(e.Element(Ram + "LineTotalAmount")!));
        lineSum.Should().Be(Amount(Summation(doc).Element(Ram + "LineTotalAmount")!)).And.Be(80m);
        Summation(doc).Element(Ram + "GrandTotalAmount")!.Value.Should().Be("95.20");
    }

    [Fact]
    public async Task GutschriftMitGegenposition_GegenpositionNegativUebrigePositiv()
    {
        var invoice = MakeInvoice(
            new InvoiceItem { Description = "Beratung", Quantity = 1, UnitPrice = -100m, VatRate = 19 },
            new InvoiceItem { Description = "Nachlass", Quantity = 1, UnitPrice = 20m, VatRate = 19 });
        invoice.Type = InvoiceType.CreditNote;

        var doc = await GenerateAsync(invoice);

        doc.Descendants(Ram + "LineTotalAmount").Take(2).Select(e => e.Value).Should().Equal("100.00", "-20.00");
        Summation(doc).Element(Ram + "GrandTotalAmount")!.Value.Should().Be("95.20");
    }

    [Fact]
    public async Task Ueberschriftspositionen_WerdenNichtAlsRechnungszeileUebertragen()
    {
        var doc = await GenerateAsync(MakeInvoice(
            new InvoiceItem { Description = "Abschnitt A", IsHeader = true, Quantity = 1, UnitPrice = 0m },
            new InvoiceItem { Description = "Beratung", Quantity = 1, UnitPrice = 100m, VatRate = 19 }));

        doc.Descendants(Ram + "IncludedSupplyChainTradeLineItem").Should().ContainSingle()
            .Which.Descendants(Ram + "Name").Single().Value.Should().Be("Beratung");
    }

    [Theory]
    [InlineData("Frankreich", "FR")]
    [InlineData("France", "FR")]
    [InlineData("Italia", "IT")]
    [InlineData(" Niederlande ", "NL")]
    public async Task Laendercode_WeitereLaenderNamen(string country, string expected)
    {
        var invoice = MakeInvoice();
        invoice.Customer!.Country = country;

        var doc = await GenerateAsync(invoice);

        doc.Descendants(Ram + "BuyerTradeParty").Single().Descendants(Ram + "CountryID").Single().Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("Atlantis")]
    [InlineData("XX")]
    public async Task UnbekanntesKundenland_ValidierungMeldetLandStattStillschweigendDE(string country)
    {
        var invoice = MakeInvoice();
        invoice.Customer!.Country = country;
        _invoiceService.Setup(s => s.GetByIdAsync(1, true, true)).ReturnsAsync(invoice);

        var (isValid, missing) = await _service.ValidateForXRechnungAsync(1);

        isValid.Should().BeFalse();
        missing.Should().ContainSingle(m => m.Contains("Kunden Land") && m.Contains(country));
    }

    [Fact]
    public async Task UnbekanntesFirmenland_ValidierungMeldetLand()
    {
        _company.Country = "Atlantis";
        _invoiceService.Setup(s => s.GetByIdAsync(1, true, true)).ReturnsAsync(MakeInvoice());

        var (_, missing) = await _service.ValidateForXRechnungAsync(1);

        missing.Should().ContainSingle(m => m.Contains("Firmen Land"));
    }
}
