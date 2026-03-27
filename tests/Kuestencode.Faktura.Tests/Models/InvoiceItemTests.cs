using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class InvoiceItemTests
{
    private static InvoiceItem MakeItem(decimal quantity, decimal unitPrice, decimal vatRate = 0) =>
        new() { Quantity = quantity, UnitPrice = unitPrice, VatRate = vatRate };

    // ─── TotalNet ─────────────────────────────────────────────────────────────

    [Fact]
    public void TotalNet_BerechnetKorrekt()
    {
        var item = MakeItem(3, 100m);
        item.TotalNet.Should().Be(300m);
    }

    [Fact]
    public void TotalNet_WirdAufZweiNachkommastellenGerundet()
    {
        var item = MakeItem(3, 10.333m);
        item.TotalNet.Should().Be(31.00m);
    }

    // ─── TotalVat ─────────────────────────────────────────────────────────────

    [Fact]
    public void TotalVat_MitMwSt19Prozent_BerechnetKorrekt()
    {
        var item = MakeItem(1, 100m, vatRate: 19m);
        item.TotalVat.Should().Be(19m);
    }

    [Fact]
    public void TotalVat_OhneMwSt_IstNull()
    {
        var item = MakeItem(2, 50m, vatRate: 0m);
        item.TotalVat.Should().Be(0m);
    }

    [Fact]
    public void TotalVat_Mit7Prozent_BerechnetKorrekt()
    {
        var item = MakeItem(1, 100m, vatRate: 7m);
        item.TotalVat.Should().Be(7m);
    }

    // ─── TotalGross ───────────────────────────────────────────────────────────

    [Fact]
    public void TotalGross_IstNetPlusMwSt()
    {
        var item = MakeItem(2, 100m, vatRate: 19m);
        item.TotalGross.Should().Be(238m);
    }

    [Fact]
    public void TotalGross_OhneMwSt_GleichNet()
    {
        var item = MakeItem(5, 20m, vatRate: 0m);
        item.TotalGross.Should().Be(100m);
    }
}
