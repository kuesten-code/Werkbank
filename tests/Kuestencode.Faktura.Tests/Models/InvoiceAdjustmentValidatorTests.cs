using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class InvoiceAdjustmentValidatorTests
{
    private static Invoice MakeInvoice(InvoiceType type = InvoiceType.Invoice)
    {
        var sign = type == InvoiceType.CreditNote ? -1 : 1;
        return new Invoice
        {
            Type = type,
            Items = [new InvoiceItem { Description = "Leistung", Quantity = 1, UnitPrice = sign * 1000m, VatRate = 19m }]
        };
    }

    [Theory]
    [InlineData(InvoiceType.Invoice)]
    [InlineData(InvoiceType.CreditNote)]
    public void OhneRabattUndAbschlaege_Gueltig(InvoiceType type)
    {
        InvoiceAdjustmentValidator.Validate(MakeInvoice(type)).Should().BeNull();
    }

    [Theory]
    [InlineData(DiscountType.Percentage, null)]
    [InlineData(DiscountType.Percentage, 0)]
    [InlineData(DiscountType.Absolute, -5)]
    public void Rabatt_OhnePositivenWert_Ungueltig(DiscountType discountType, int? value)
    {
        var invoice = MakeInvoice();
        invoice.DiscountType = discountType;
        invoice.DiscountValue = value;

        InvoiceAdjustmentValidator.Validate(invoice).Should().Be("Rabatt muss größer als 0 sein.");
    }

    [Fact]
    public void ProzentRabatt_UeberHundert_Ungueltig()
    {
        var invoice = MakeInvoice();
        invoice.DiscountType = DiscountType.Percentage;
        invoice.DiscountValue = 101m;

        InvoiceAdjustmentValidator.Validate(invoice).Should().Contain("100%");
    }

    [Theory]
    [InlineData(InvoiceType.Invoice)]
    [InlineData(InvoiceType.CreditNote)]
    public void AbsoluterRabatt_UeberNettosumme_Ungueltig(InvoiceType type)
    {
        var invoice = MakeInvoice(type);
        invoice.DiscountType = DiscountType.Absolute;
        invoice.DiscountValue = 1000.01m;

        InvoiceAdjustmentValidator.Validate(invoice).Should().Contain("Nettosumme");
    }

    [Theory]
    [InlineData(InvoiceType.Invoice)]
    [InlineData(InvoiceType.CreditNote)]
    public void AbsoluterRabatt_InnerhalbNettosumme_Gueltig(InvoiceType type)
    {
        var invoice = MakeInvoice(type);
        invoice.DiscountType = DiscountType.Absolute;
        invoice.DiscountValue = 1000m;

        InvoiceAdjustmentValidator.Validate(invoice).Should().BeNull();
    }

    [Fact]
    public void Abschlag_OhneBetrag_Ungueltig()
    {
        var invoice = MakeInvoice();
        invoice.DownPayments.Add(new DownPayment { Description = "AR-1", Amount = 0m });

        InvoiceAdjustmentValidator.Validate(invoice).Should().Be("Abschlag-Betrag muss größer als 0 sein.");
    }

    [Fact]
    public void Abschlag_OhneBeschreibung_Ungueltig()
    {
        var invoice = MakeInvoice();
        invoice.DownPayments.Add(new DownPayment { Description = " ", Amount = 100m });

        InvoiceAdjustmentValidator.Validate(invoice).Should().Be("Beschreibung für Abschläge ist erforderlich.");
    }

    [Theory]
    [InlineData(InvoiceType.Invoice)]
    [InlineData(InvoiceType.CreditNote)]
    public void Abschlaege_UeberBruttosumme_Ungueltig(InvoiceType type)
    {
        var invoice = MakeInvoice(type);
        invoice.DownPayments.Add(new DownPayment { Description = "AR-1", Amount = 1190.01m });

        InvoiceAdjustmentValidator.Validate(invoice).Should().Contain("Bruttosumme");
    }

    [Theory]
    [InlineData(InvoiceType.Invoice)]
    [InlineData(InvoiceType.CreditNote)]
    public void Abschlaege_BisBruttosumme_Gueltig(InvoiceType type)
    {
        var invoice = MakeInvoice(type);
        invoice.DownPayments.Add(new DownPayment { Description = "AR-1", Amount = 1190m });

        InvoiceAdjustmentValidator.Validate(invoice).Should().BeNull();
    }
}
