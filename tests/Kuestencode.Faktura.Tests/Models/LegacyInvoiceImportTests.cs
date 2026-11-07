using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class LegacyInvoiceImportTests
{
    private static LegacyInvoiceImport MakeImport(decimal net, decimal vatRate, string number = "RE-2025-0815") =>
        new(number, 1, new DateTime(2025, 11, 3), null, net, vatRate, 0m, null, "x.pdf", [1], null);

    [Theory]
    [InlineData(1000, 19, 1190)]
    [InlineData(1000, 7, 1070)]
    [InlineData(1000, 0, 1000)]
    [InlineData(99.99, 19, 118.99)]
    public void GrossAmount_RundetWieRechnungsposition(decimal net, decimal vatRate, decimal expectedGross)
    {
        MakeImport(net, vatRate).GrossAmount.Should().Be(expectedGross);
    }

    [Fact]
    public void ToSummaryItem_BildetSummenAlsEinePositionAb()
    {
        var item = MakeImport(250m, 7m, number: " RE-2025-0815 ").ToSummaryItem();

        item.Position.Should().Be(1);
        item.Quantity.Should().Be(1);
        item.UnitPrice.Should().Be(250m);
        item.VatRate.Should().Be(7m);
        item.IsHeader.Should().BeFalse();
        item.Description.Should().Be("Offener Posten aus Vorsystem, Rechnung RE-2025-0815");
    }
}
