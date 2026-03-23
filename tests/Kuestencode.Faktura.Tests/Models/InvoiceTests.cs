using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class InvoiceTests
{
    private static InvoiceItem MakeItem(decimal quantity, decimal unitPrice, decimal vatRate = 19m) =>
        new() { Quantity = quantity, UnitPrice = unitPrice, VatRate = vatRate };

    private static Invoice MakeInvoice(params InvoiceItem[] items)
    {
        var inv = new Invoice { InvoiceNumber = "R-2026-0001", CustomerId = 1 };
        inv.Items.AddRange(items);
        return inv;
    }

    // ─── TotalNet ─────────────────────────────────────────────────────────────

    [Fact]
    public void TotalNet_SummiertAllePositionen()
    {
        var inv = MakeInvoice(MakeItem(2, 100m), MakeItem(1, 50m));
        inv.TotalNet.Should().Be(250m);
    }

    [Fact]
    public void TotalNet_OhnePositionen_IstNull()
    {
        MakeInvoice().TotalNet.Should().Be(0m);
    }

    // ─── DiscountAmount ───────────────────────────────────────────────────────

    [Fact]
    public void DiscountAmount_OhneRabatt_IstNull()
    {
        var inv = MakeInvoice(MakeItem(1, 100m));
        inv.DiscountType = DiscountType.None;
        inv.DiscountAmount.Should().Be(0m);
    }

    [Fact]
    public void DiscountAmount_ProzentRabatt10_BerechnetKorrekt()
    {
        var inv = MakeInvoice(MakeItem(1, 200m));
        inv.DiscountType = DiscountType.Percentage;
        inv.DiscountValue = 10m;
        inv.DiscountAmount.Should().Be(20m);
    }

    [Fact]
    public void DiscountAmount_AbsoluterRabatt_GibtWertZurueck()
    {
        var inv = MakeInvoice(MakeItem(1, 200m));
        inv.DiscountType = DiscountType.Absolute;
        inv.DiscountValue = 30m;
        inv.DiscountAmount.Should().Be(30m);
    }

    [Fact]
    public void DiscountAmount_ProzentOhneWert_IstNull()
    {
        var inv = MakeInvoice(MakeItem(1, 100m));
        inv.DiscountType = DiscountType.Percentage;
        inv.DiscountValue = null;
        inv.DiscountAmount.Should().Be(0m);
    }

    // ─── TotalNetAfterDiscount ────────────────────────────────────────────────

    [Fact]
    public void TotalNetAfterDiscount_MitProzentRabatt_KorrektBerechnet()
    {
        var inv = MakeInvoice(MakeItem(1, 100m));
        inv.DiscountType = DiscountType.Percentage;
        inv.DiscountValue = 10m;
        inv.TotalNetAfterDiscount.Should().Be(90m);
    }

    [Fact]
    public void TotalNetAfterDiscount_OhneRabatt_GleichTotalNet()
    {
        var inv = MakeInvoice(MakeItem(2, 50m));
        inv.TotalNetAfterDiscount.Should().Be(100m);
    }

    // ─── TotalVat ─────────────────────────────────────────────────────────────

    [Fact]
    public void TotalVat_OhneRabatt_SummiertMwSt()
    {
        var inv = MakeInvoice(MakeItem(1, 100m, vatRate: 19m));
        inv.TotalVat.Should().Be(19m);
    }

    [Fact]
    public void TotalVat_MitRabatt_WirdProportionalReduziert()
    {
        // 100 net, 10% discount → 90 net after discount → ratio 0.9 → VAT = 19 * 0.9 = 17.1
        var inv = MakeInvoice(MakeItem(1, 100m, vatRate: 19m));
        inv.DiscountType = DiscountType.Percentage;
        inv.DiscountValue = 10m;
        inv.TotalVat.Should().BeApproximately(17.1m, 0.01m);
    }

    [Fact]
    public void TotalVat_OhnePositionen_IstNull()
    {
        MakeInvoice().TotalVat.Should().Be(0m);
    }

    // ─── TotalGross ───────────────────────────────────────────────────────────

    [Fact]
    public void TotalGross_IstNetNachRabattPlusMwSt()
    {
        var inv = MakeInvoice(MakeItem(1, 100m, vatRate: 0m));
        inv.TotalGross.Should().Be(100m);
    }

    // ─── TotalDownPayments ────────────────────────────────────────────────────

    [Fact]
    public void TotalDownPayments_SummiertAbschlagszahlungen()
    {
        var inv = MakeInvoice();
        inv.DownPayments.Add(new DownPayment { Amount = 100m });
        inv.DownPayments.Add(new DownPayment { Amount = 50m });
        inv.TotalDownPayments.Should().Be(150m);
    }

    [Fact]
    public void TotalDownPayments_OhneAbschlag_IstNull()
    {
        MakeInvoice().TotalDownPayments.Should().Be(0m);
    }

    // ─── AmountDue ────────────────────────────────────────────────────────────

    [Fact]
    public void AmountDue_IstBruttoMinusAbschlagszahlungen()
    {
        var inv = MakeInvoice(MakeItem(1, 100m, vatRate: 0m));
        inv.DownPayments.Add(new DownPayment { Amount = 30m });
        inv.AmountDue.Should().Be(70m);
    }

    [Fact]
    public void AmountDue_OhneAbschlag_GleichBrutto()
    {
        var inv = MakeInvoice(MakeItem(1, 200m, vatRate: 0m));
        inv.AmountDue.Should().Be(200m);
    }
}
