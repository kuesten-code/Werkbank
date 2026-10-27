using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class InvoiceSkontoTests
{
    private static Invoice MakeInvoice(decimal net = 1000m, InvoiceType type = InvoiceType.Invoice) => new()
    {
        Type = type,
        InvoiceDate = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc),
        Items = [new InvoiceItem { Quantity = 1, UnitPrice = net, VatRate = 19m }]
    };

    private static Invoice WithSkonto(Invoice invoice, DiscountType type, decimal value, int? days = 10)
    {
        invoice.SkontoType = type;
        invoice.SkontoValue = value;
        invoice.SkontoDays = days;
        return invoice;
    }

    [Fact]
    public void SkontoAmount_OhneSkonto_IstNull()
    {
        MakeInvoice().SkontoAmount.Should().Be(0m);
    }

    [Fact]
    public void SkontoAmount_TypOhneWert_IstNull()
    {
        var invoice = MakeInvoice();
        invoice.SkontoType = DiscountType.Percentage;

        invoice.SkontoAmount.Should().Be(0m);
    }

    [Fact]
    public void SkontoAmount_Prozent_BeziehtSichAufBruttoZahlbetrag()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);

        invoice.SkontoAmount.Should().Be(23.80m);
        invoice.AmountDueWithSkonto.Should().Be(1166.20m);
    }

    [Fact]
    public void SkontoAmount_Prozent_WirdAufCentGerundet()
    {
        var invoice = WithSkonto(MakeInvoice(333.33m), DiscountType.Percentage, 3m);

        invoice.SkontoAmount.Should().Be(11.90m);
    }

    [Fact]
    public void SkontoAmount_MitAbschlaegen_NurAufRestbetrag()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        invoice.DownPayments.Add(new DownPayment { Description = "AR-1", Amount = 595m });

        invoice.SkontoAmount.Should().Be(11.90m);
    }

    [Fact]
    public void SkontoAmount_MitRabatt_BeziehtSichAufRabattiertenBetrag()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        invoice.DiscountType = DiscountType.Percentage;
        invoice.DiscountValue = 10m;

        invoice.AmountDue.Should().Be(1071m);
        invoice.SkontoAmount.Should().Be(21.42m);
    }

    [Fact]
    public void SkontoAmount_Absolut_GibtBetragZurueck()
    {
        WithSkonto(MakeInvoice(), DiscountType.Absolute, 25m).SkontoAmount.Should().Be(25m);
    }

    [Fact]
    public void SkontoAmount_AbsolutAufGutschrift_TraegtBelegvorzeichen()
    {
        WithSkonto(MakeInvoice(-1000m, InvoiceType.CreditNote), DiscountType.Absolute, 25m).SkontoAmount.Should().Be(-25m);
    }

    [Fact]
    public void SkontoDeadline_IstRechnungsdatumPlusFrist()
    {
        WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m, 10).SkontoDeadline
            .Should().Be(new DateTime(2026, 11, 6));
    }

    [Fact]
    public void SkontoDeadline_OhneFrist_IstNull()
    {
        WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m, null).SkontoDeadline.Should().BeNull();
    }

    [Fact]
    public void NichtGewaehrt_RestbetragUndBelegbetragUnveraendert()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        invoice.Payments.Add(new InvoicePayment { Amount = 1166.20m });

        invoice.GrantedSkonto.Should().Be(0m);
        invoice.SettlementAmount.Should().Be(1190m);
        invoice.RemainingAmount.Should().Be(23.80m);
        invoice.DocumentAmount.Should().Be(1190m);
    }

    [Fact]
    public void Gewaehrt_RechnungIstMitSkontobetragBeglichen()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        invoice.SkontoApplied = true;
        invoice.Payments.Add(new InvoicePayment { Amount = 1166.20m });

        invoice.GrantedSkonto.Should().Be(23.80m);
        invoice.SettlementAmount.Should().Be(1166.20m);
        invoice.RemainingAmount.Should().Be(0m);
    }

    [Fact]
    public void Gewaehrt_RechnungsbelegZeigtWeiterVollenBetrag()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        invoice.SkontoApplied = true;

        invoice.DocumentAmount.Should().Be(1190m);
    }

    [Fact]
    public void Gutschrift_MitGewaehrtemSkonto_BelegbetragIstErstattung()
    {
        var creditNote = WithSkonto(MakeInvoice(-1000m, InvoiceType.CreditNote), DiscountType.Percentage, 2m, null);
        creditNote.SkontoApplied = true;

        creditNote.GrantedSkonto.Should().Be(-23.80m);
        creditNote.DocumentAmount.Should().Be(-1166.20m);
        creditNote.RemainingAmount.Should().Be(-1166.20m);
    }

    [Fact]
    public void TotalPaidNet_SkontozahlungMindertNettoAnteilig()
    {
        var invoice = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        invoice.SkontoApplied = true;
        invoice.Payments.Add(new InvoicePayment { Amount = 1166.20m });

        invoice.TotalPaidNet.Should().Be(980m);
    }

    [Fact]
    public void DiscountRatio_OhnePositionen_IstNull()
    {
        new Invoice().DiscountRatio.Should().Be(0m);
    }

    [Fact]
    public void DiscountRatio_MitRabatt_IstAnteilNachRabatt()
    {
        var invoice = MakeInvoice();
        invoice.DiscountType = DiscountType.Absolute;
        invoice.DiscountValue = 250m;

        invoice.DiscountRatio.Should().Be(0.75m);
    }

    // ─── AdoptGrantedSkonto ───────────────────────────────────────────────────

    [Fact]
    public void AdoptGrantedSkonto_GewaehrtesProzentSkonto_WirdUebernommen()
    {
        var source = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        source.SkontoApplied = true;
        var creditNote = MakeInvoice(-1000m, InvoiceType.CreditNote);

        creditNote.AdoptGrantedSkonto(source);

        creditNote.SkontoType.Should().Be(DiscountType.Percentage);
        creditNote.SkontoValue.Should().Be(2m);
        creditNote.SkontoDays.Should().BeNull();
        creditNote.SkontoApplied.Should().BeTrue();
        creditNote.DocumentAmount.Should().Be(-1166.20m);
    }

    [Fact]
    public void AdoptGrantedSkonto_AbsolutesSkonto_ErstattungCentgenau()
    {
        var source = WithSkonto(MakeInvoice(), DiscountType.Absolute, 25m);
        source.SkontoApplied = true;
        var creditNote = MakeInvoice(-1000m, InvoiceType.CreditNote);

        creditNote.AdoptGrantedSkonto(source);

        creditNote.SkontoType.Should().Be(DiscountType.Percentage);
        creditNote.GrantedSkonto.Should().Be(-25m);
        creditNote.DocumentAmount.Should().Be(-1165m);
    }

    [Fact]
    public void AdoptGrantedSkonto_Teilgutschrift_WirdAnteiligGemindert()
    {
        var source = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        source.SkontoApplied = true;
        var creditNote = MakeInvoice(-500m, InvoiceType.CreditNote);

        creditNote.AdoptGrantedSkonto(source);

        creditNote.GrantedSkonto.Should().Be(-11.90m);
    }

    [Fact]
    public void AdoptGrantedSkonto_NichtGewaehrt_SetztSkontoZurueck()
    {
        var source = WithSkonto(MakeInvoice(), DiscountType.Percentage, 2m);
        var creditNote = WithSkonto(MakeInvoice(-1000m, InvoiceType.CreditNote), DiscountType.Percentage, 3m);
        creditNote.SkontoApplied = true;

        creditNote.AdoptGrantedSkonto(source);

        creditNote.SkontoType.Should().Be(DiscountType.None);
        creditNote.SkontoValue.Should().BeNull();
        creditNote.SkontoDays.Should().BeNull();
        creditNote.SkontoApplied.Should().BeFalse();
    }

    [Fact]
    public void AdoptGrantedSkonto_OhneBezugsrechnung_SetztSkontoZurueck()
    {
        var creditNote = WithSkonto(MakeInvoice(-1000m, InvoiceType.CreditNote), DiscountType.Percentage, 2m);
        creditNote.SkontoApplied = true;

        creditNote.AdoptGrantedSkonto(null);

        creditNote.SkontoType.Should().Be(DiscountType.None);
        creditNote.SkontoApplied.Should().BeFalse();
    }
}
