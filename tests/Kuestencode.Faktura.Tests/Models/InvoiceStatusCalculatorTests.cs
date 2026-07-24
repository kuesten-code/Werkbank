using FluentAssertions;
using Kuestencode.Faktura.Models;
using Xunit;

namespace Kuestencode.Faktura.Tests.Models;

public class InvoiceStatusCalculatorTests
{
    [Fact]
    public void Calculate_PositiverBetragVollBezahlt_GibtPaidZurueck()
    {
        var result = InvoiceStatusCalculator.Calculate(totalGross: 119m, totalPaid: 119m, dueDate: null);
        result.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void Calculate_NegativerBetragVollAusgeglichen_GibtPaidZurueck()
    {
        // Gutschrift: negativer Bruttobetrag, vollständig durch eine negative Zahlung ausgeglichen
        var result = InvoiceStatusCalculator.Calculate(totalGross: -119m, totalPaid: -119m, dueDate: null);
        result.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void Calculate_NegativerBetragTeilweiseAusgeglichen_GibtPartiallyPaidZurueck()
    {
        var result = InvoiceStatusCalculator.Calculate(totalGross: -119m, totalPaid: -50m, dueDate: null);
        result.Should().Be(InvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public void Calculate_KeineZahlungKeinFaelligkeitsdatum_GibtSentZurueck()
    {
        var result = InvoiceStatusCalculator.Calculate(totalGross: 119m, totalPaid: 0m, dueDate: null);
        result.Should().Be(InvoiceStatus.Sent);
    }

    [Fact]
    public void Calculate_KeineZahlungUeberfaellig_GibtOverdueZurueck()
    {
        var result = InvoiceStatusCalculator.Calculate(totalGross: 119m, totalPaid: 0m, dueDate: DateTime.UtcNow.AddDays(-1));
        result.Should().Be(InvoiceStatus.Overdue);
    }
}
