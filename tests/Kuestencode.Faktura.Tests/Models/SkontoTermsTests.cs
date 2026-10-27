using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class SkontoTermsTests
{
    private static Invoice MakeInvoice(DiscountType skontoType, decimal? skontoValue, int? skontoDays, InvoiceType type = InvoiceType.Invoice) => new()
    {
        Type = type,
        InvoiceDate = new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc),
        SkontoType = skontoType,
        SkontoValue = skontoValue,
        SkontoDays = skontoDays,
        Items = [new InvoiceItem { Quantity = 1, UnitPrice = type == InvoiceType.CreditNote ? -1000m : 1000m, VatRate = 19m }]
    };

    [Fact]
    public void Prozent_NenntSatzBetragFristUndZahlbetrag()
    {
        SkontoTerms.Describe(MakeInvoice(DiscountType.Percentage, 2m, 10)).Should().Be(
            "Bei Zahlung bis zum 06.11.2026 gewähren wir 2 % Skonto (23,80 €). Zahlbetrag dann: 1.166,20 €");
    }

    [Fact]
    public void Absolut_NenntBetrag()
    {
        SkontoTerms.Describe(MakeInvoice(DiscountType.Absolute, 25m, 14)).Should().Be(
            "Bei Zahlung bis zum 10.11.2026 gewähren wir 25,00 € Skonto. Zahlbetrag dann: 1.165,00 €");
    }

    [Fact]
    public void OhneSkonto_KeinText()
    {
        SkontoTerms.Describe(MakeInvoice(DiscountType.None, null, null)).Should().BeNull();
    }

    [Fact]
    public void OhneFrist_KeinText()
    {
        SkontoTerms.Describe(MakeInvoice(DiscountType.Percentage, 2m, null)).Should().BeNull();
    }

    [Fact]
    public void Gutschrift_KeinAngebot()
    {
        SkontoTerms.Describe(MakeInvoice(DiscountType.Percentage, 2m, 10, InvoiceType.CreditNote)).Should().BeNull();
    }
}
