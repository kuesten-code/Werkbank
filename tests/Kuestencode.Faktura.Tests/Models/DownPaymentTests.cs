using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class DownPaymentTests
{
    [Fact]
    public void ApplyNetAmount_Mit19Prozent_BerechnetBrutto()
    {
        var downPayment = new DownPayment { VatRate = 19m };

        downPayment.ApplyNetAmount(1000m);

        downPayment.NetAmount.Should().Be(1000m);
        downPayment.Amount.Should().Be(1190m);
        downPayment.VatAmount.Should().Be(190m);
    }

    [Fact]
    public void ApplyNetAmount_RundetBruttoKaufmaennisch()
    {
        var downPayment = new DownPayment { VatRate = 19m };

        downPayment.ApplyNetAmount(0.5m);

        downPayment.Amount.Should().Be(0.60m);
    }

    [Fact]
    public void ApplyGrossAmount_Mit19Prozent_BerechnetNetto()
    {
        var downPayment = new DownPayment { VatRate = 19m };

        downPayment.ApplyGrossAmount(2800m);

        downPayment.Amount.Should().Be(2800m);
        downPayment.NetAmount.Should().Be(2352.94m);
        downPayment.VatAmount.Should().Be(447.06m);
    }

    [Fact]
    public void ApplyGrossAmount_OhneMwSt_NettoGleichBrutto()
    {
        var downPayment = new DownPayment { VatRate = 0m };

        downPayment.ApplyGrossAmount(500m);

        downPayment.NetAmount.Should().Be(500m);
        downPayment.VatAmount.Should().Be(0m);
    }

    [Fact]
    public void ApplyVatRate_BehaeltNettoUndBerechnetBruttoNeu()
    {
        var downPayment = new DownPayment { VatRate = 19m };
        downPayment.ApplyNetAmount(1000m);

        downPayment.ApplyVatRate(7m);

        downPayment.VatRate.Should().Be(7m);
        downPayment.NetAmount.Should().Be(1000m);
        downPayment.Amount.Should().Be(1070m);
    }

    [Fact]
    public void ApplySourceInvoice_UebernimmtNettoBruttoUndSteuersatz()
    {
        var sourceInvoice = new Invoice
        {
            Items = [new InvoiceItem { Quantity = 1, UnitPrice = 10000m, VatRate = 19m }]
        };
        var downPayment = new DownPayment();

        downPayment.ApplySourceInvoice(sourceInvoice);

        downPayment.NetAmount.Should().Be(10000m);
        downPayment.Amount.Should().Be(11900m);
        downPayment.VatRate.Should().Be(19m);
    }

    [Fact]
    public void ApplySourceInvoice_GemischteSteuersaetze_ErgibtEffektivenSatz()
    {
        var sourceInvoice = new Invoice
        {
            Items =
            [
                new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 19m },
                new InvoiceItem { Quantity = 1, UnitPrice = 100m, VatRate = 7m }
            ]
        };
        var downPayment = new DownPayment();

        downPayment.ApplySourceInvoice(sourceInvoice);

        downPayment.NetAmount.Should().Be(200m);
        downPayment.Amount.Should().Be(226m);
        downPayment.VatRate.Should().Be(13m);
    }

    [Fact]
    public void ApplySourceInvoice_OhnePositionen_SteuersatzNull()
    {
        var downPayment = new DownPayment { VatRate = 19m };

        downPayment.ApplySourceInvoice(new Invoice());

        downPayment.NetAmount.Should().Be(0m);
        downPayment.Amount.Should().Be(0m);
        downPayment.VatRate.Should().Be(0m);
    }

    // ─── SelectSourceInvoice ──────────────────────────────────────────────────

    private static Invoice MakeSourceInvoice() => new()
    {
        Id = 5,
        InvoiceNumber = "AR-2026-0005",
        Items = [new InvoiceItem { Quantity = 1, UnitPrice = 500m, VatRate = 19m }]
    };

    [Fact]
    public void SelectSourceInvoice_LeererAbschlag_UebernimmtNummerUndBetraege()
    {
        var downPayment = new DownPayment();

        downPayment.SelectSourceInvoice(MakeSourceInvoice());

        downPayment.SourceInvoiceId.Should().Be(5);
        downPayment.Description.Should().Be("AR-2026-0005");
        downPayment.NetAmount.Should().Be(500m);
        downPayment.Amount.Should().Be(595m);
    }

    [Fact]
    public void SelectSourceInvoice_ErfassteWerte_BleibenErhalten()
    {
        var downPayment = new DownPayment { Description = "Anzahlung", NetAmount = 100m, VatRate = 19m, Amount = 119m };

        downPayment.SelectSourceInvoice(MakeSourceInvoice());

        downPayment.SourceInvoiceId.Should().Be(5);
        downPayment.Description.Should().Be("Anzahlung");
        downPayment.Amount.Should().Be(119m);
    }

    [Fact]
    public void SelectSourceInvoice_Null_EntferntVerknuepfung()
    {
        var downPayment = new DownPayment { SourceInvoiceId = 5, SourceInvoice = MakeSourceInvoice(), Amount = 595m };

        downPayment.SelectSourceInvoice(null);

        downPayment.SourceInvoiceId.Should().BeNull();
        downPayment.SourceInvoice.Should().BeNull();
        downPayment.Amount.Should().Be(595m);
    }
}
