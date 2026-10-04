using System.Text;
using FluentAssertions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services.Pdf;

/// <summary>
/// Prüft die steuerrelevanten Pflichtangaben im gerenderten PDF (§14 Abs. 4 UStG) über alle Layouts:
/// MwSt-Zeile, §19-/§13b-Hinweis, Rabatt, Abschläge und Gutschrift-Darstellung.
/// </summary>
public class PdfTaxContentTests : IClassFixture<FakturaWebApplicationFactory>
{
    private readonly FakturaWebApplicationFactory _factory;

    public PdfTaxContentTests(FakturaWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<PdfLayout> Layouts => new() { PdfLayout.Klar, PdfLayout.Strukturiert, PdfLayout.Betont };

    private static Company MakeCompany(PdfLayout layout, bool kleinunternehmer = false) => new()
    {
        OwnerFullName = "Erika Musterfrau",
        Address = "Hafenstraße 1",
        PostalCode = "20457",
        City = "Hamburg",
        BankName = "VR Bank Nord eG",
        BankAccount = "DE23217635420032798824",
        Email = "info@kuestencode.de",
        IsKleinunternehmer = kleinunternehmer,
        PdfLayout = layout
    };

    private static Invoice MakeInvoice(decimal vatRate = 19) => new()
    {
        InvoiceNumber = "R-2026-0042",
        InvoiceDate = new DateTime(2026, 3, 15),
        DueDate = new DateTime(2026, 3, 29),
        CustomerId = 1,
        Customer = new Customer { Name = "Nordlicht Media", Address = "Speicherstadt 5", City = "Hamburg", PostalCode = "20457" },
        Items = [new InvoiceItem { Description = "Beratung", Quantity = 2, UnitPrice = 100m, VatRate = vatRate }]
    };

    private string Render(Invoice invoice, Company company)
    {
        using var scope = _factory.Services.CreateScope();
        var bytes = scope.ServiceProvider.GetRequiredService<IPdfGeneratorService>().GeneratePdfWithCompany(invoice, company);

        using var pdf = new PdfDocument(new PdfReader(new MemoryStream(bytes)));
        var text = new StringBuilder();
        for (var page = 1; page <= pdf.GetNumberOfPages(); page++)
        {
            text.AppendLine(PdfTextExtractor.GetTextFromPage(pdf.GetPage(page)));
        }
        return text.ToString();
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Regelbesteuerung_ZeigtMwStSatzUndBetragOhneSteuerhinweis(PdfLayout layout)
    {
        var text = Render(MakeInvoice(), MakeCompany(layout));

        text.Should().Contain("MwSt (19%):");
        text.Should().Contain("38,00 €");
        text.Should().Contain("238,00 €");
        text.Should().NotContain("§ 19 UStG");
        text.Should().NotContain("§ 13b UStG");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Kleinunternehmer_ZeigtNullProzentUndParagraph19Hinweis(PdfLayout layout)
    {
        var text = Render(MakeInvoice(vatRate: 0), MakeCompany(layout, kleinunternehmer: true));

        text.Should().Contain("MwSt (0% §19 UStG):");
        text.Should().Contain("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.");
        text.Should().NotContain("§ 13b UStG");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void ReverseCharge_ZeigtNullProzentUndParagraph13bHinweis(PdfLayout layout)
    {
        var invoice = MakeInvoice(vatRate: 0);
        invoice.IsReverseCharge = true;

        var text = Render(invoice, MakeCompany(layout));

        text.Should().Contain("MwSt (0%, §13b UStG):");
        text.Should().Contain("Steuerschuldnerschaft des Leistungsempfängers gemäß § 13b UStG");
        text.Should().NotContain("§ 19 UStG");
    }

    [Fact]
    public void KleinunternehmerMitReverseChargeFlag_NurParagraph19Hinweis()
    {
        var invoice = MakeInvoice(vatRate: 0);
        invoice.IsReverseCharge = true;

        var text = Render(invoice, MakeCompany(PdfLayout.Klar, kleinunternehmer: true));

        text.Should().Contain("MwSt (0% §19 UStG):");
        text.Should().NotContain("§ 13b UStG");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Rabatt_ZeigtRabattZeileUndZwischensumme(PdfLayout layout)
    {
        var invoice = MakeInvoice();
        invoice.DiscountType = DiscountType.Percentage;
        invoice.DiscountValue = 10;

        var text = Render(invoice, MakeCompany(layout));

        text.Should().Contain("Rabatt (10%):");
        text.Should().Contain("-20,00 €");
        text.Should().Contain("Zwischensumme:");
        text.Should().Contain("180,00 €");
        text.Should().Contain("34,20 €");
        text.Should().Contain("214,20 €");
    }

    [Fact]
    public void AbsoluterRabatt_ZeigtRabattOhneProzentangabe()
    {
        var invoice = MakeInvoice();
        invoice.DiscountType = DiscountType.Absolute;
        invoice.DiscountValue = 50;

        var text = Render(invoice, MakeCompany(PdfLayout.Klar));

        text.Should().Contain("Rabatt:");
        text.Should().Contain("-50,00 €");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Abschlag_ZeigtZuZahlenAlsBruttoAbzueglichAbschlag(PdfLayout layout)
    {
        var invoice = MakeInvoice();
        invoice.DownPayments.Add(new DownPayment { Description = "Abschlag 1", Amount = 100m, NetAmount = 84.03m, VatRate = 19 });

        var text = Render(invoice, MakeCompany(layout));

        text.Should().Contain("Zu zahlen:");
        text.Should().Contain("138,00 €");
    }

    [Fact]
    public void OhneAbschlag_KeineZuZahlenZeile()
    {
        var text = Render(MakeInvoice(), MakeCompany(PdfLayout.Klar));

        text.Should().NotContain("Zu zahlen:");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void Gutschrift_ZeigtBetraegePositivMitGutschriftLabel(PdfLayout layout)
    {
        var invoice = MakeInvoice();
        invoice.Type = InvoiceType.CreditNote;
        invoice.Items[0].UnitPrice = -100m;

        var text = Render(invoice, MakeCompany(layout));

        text.Should().Contain("Gutschriftbetrag:");
        text.Should().Contain("238,00 €");
        text.Should().NotContain("Bruttosumme:");
    }

    [Theory]
    [MemberData(nameof(Layouts))]
    public void GemischteSteuersaetze_WeistMwStJeSatzAus(PdfLayout layout)
    {
        var invoice = MakeInvoice();
        invoice.Items.Add(new InvoiceItem { Description = "Fachbuch", Quantity = 1, UnitPrice = 100m, VatRate = 7 });

        var text = Render(invoice, MakeCompany(layout));

        text.Should().Contain("MwSt (19%):");
        text.Should().Contain("38,00 €");
        text.Should().Contain("MwSt (7%):");
        text.Should().Contain("7,00 €");
        text.Should().Contain("345,00 €");
    }

    [Fact]
    public void Steuersatz_AusDatenbankMitNachkommastellen_OhneNachkommastellenAngezeigt()
    {
        var invoice = MakeInvoice(vatRate: 19.00m);

        var text = Render(invoice, MakeCompany(PdfLayout.Klar));

        text.Should().Contain("MwSt (19%):");
    }
}
