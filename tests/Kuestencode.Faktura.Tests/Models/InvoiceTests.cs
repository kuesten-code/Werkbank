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

    // ─── TotalPaidNet ─────────────────────────────────────────────────────────

    [Fact]
    public void TotalPaidNet_Teilzahlung_IstAnteiligerNettobetrag()
    {
        var inv = MakeInvoice(MakeItem(1, 1000m));
        inv.Payments.Add(new InvoicePayment { Amount = 595m });
        inv.TotalPaidNet.Should().Be(500m);
    }

    [Fact]
    public void TotalPaidNet_MehrereTeilzahlungen_WerdenSummiert()
    {
        var inv = MakeInvoice(MakeItem(1, 1000m));
        inv.Payments.Add(new InvoicePayment { Amount = 238m });
        inv.Payments.Add(new InvoicePayment { Amount = 357m });
        inv.TotalPaidNet.Should().Be(500m);
    }

    [Fact]
    public void TotalPaidNet_VollstaendigBezahlt_GleichNettoNachRabatt()
    {
        var inv = MakeInvoice(MakeItem(1, 1000m));
        inv.DiscountType = DiscountType.Percentage;
        inv.DiscountValue = 10m;
        inv.Payments.Add(new InvoicePayment { Amount = inv.TotalGross });
        inv.TotalPaidNet.Should().Be(900m);
    }

    [Fact]
    public void TotalPaidNet_OhneZahlung_IstNull()
    {
        MakeInvoice(MakeItem(1, 1000m)).TotalPaidNet.Should().Be(0m);
    }

    [Fact]
    public void TotalPaidNet_OhnePositionen_IstNull()
    {
        var inv = MakeInvoice();
        inv.Payments.Add(new InvoicePayment { Amount = 100m });
        inv.TotalPaidNet.Should().Be(0m);
    }

    [Fact]
    public void TotalPaidNet_TeilweiseErstatteteGutschrift_IstAnteiligNegativ()
    {
        var inv = MakeInvoice(MakeItem(-1, 1000m));
        inv.Payments.Add(new InvoicePayment { Amount = -595m });
        inv.TotalPaidNet.Should().Be(-500m);
    }

    // ─── EffectiveDueDate ─────────────────────────────────────────────────────

    [Fact]
    public void EffectiveDueDate_OhneNeuesZahlungsziel_IstFaelligkeitsdatum()
    {
        var inv = MakeInvoice();
        inv.DueDate = new DateTime(2026, 3, 1);

        inv.EffectiveDueDate.Should().Be(new DateTime(2026, 3, 1));
    }

    [Fact]
    public void EffectiveDueDate_MitNeuemZahlungsziel_IstNeuesZahlungsziel()
    {
        var inv = MakeInvoice();
        inv.DueDate = new DateTime(2026, 3, 1);
        inv.RevisedDueDate = new DateTime(2026, 4, 15);

        inv.EffectiveDueDate.Should().Be(new DateTime(2026, 4, 15));
    }

    // ─── RemainingAmount / VatBreakdown ───────────────────────────────────────

    [Fact]
    public void RemainingAmount_SchlussrechnungMitAbschlag_IstZuZahlenAbzueglichZahlungen()
    {
        var invoice = new Invoice
        {
            Items = [new InvoiceItem { Quantity = 1, UnitPrice = 1000m, VatRate = 19 }],
            DownPayments = [new DownPayment { Amount = 595m }],
            Payments = [new InvoicePayment { Amount = 200m }]
        };

        invoice.AmountDue.Should().Be(595m);
        invoice.RemainingAmount.Should().Be(395m);
    }

    [Fact]
    public void VatBreakdown_GruppiertNachSteuersatzAbsteigendOhneUeberschriften()
    {
        var invoice = new Invoice
        {
            Items =
            [
                new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 7 },
                new InvoiceItem { Quantity = 1, UnitPrice = 200m, VatRate = 19 },
                new InvoiceItem { Quantity = 2, UnitPrice = 50m, VatRate = 19 },
                new InvoiceItem { IsHeader = true, Quantity = 1, UnitPrice = 999m, VatRate = 0 }
            ]
        };

        invoice.VatBreakdown.Should().Equal(
            new VatGroup(19, 300m, 300m, 57m),
            new VatGroup(7, 100m, 100m, 7m));
    }

    [Fact]
    public void VatBreakdown_RabattWirdAnteiligVerteiltUndSummeEntsprichtTotalVat()
    {
        var invoice = new Invoice
        {
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            Items =
            [
                new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 7 },
                new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 19 }
            ]
        };

        invoice.VatBreakdown.Select(g => (g.Rate, g.LineNet, g.NetAfterDiscount, g.Vat))
            .Should().Equal((19m, 100m, 90m, 17.1m), (7m, 100m, 90m, 6.3m));
        invoice.VatBreakdown.Sum(g => g.Vat).Should().Be(invoice.TotalVat);
    }

    [Fact]
    public void VatBreakdown_OhnePositionen_Leer()
    {
        new Invoice().VatBreakdown.Should().BeEmpty();
    }
}
