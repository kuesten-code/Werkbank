namespace Kuestencode.Faktura.Models;

public static class InvoiceStatusCalculator
{
    public static InvoiceStatus Calculate(decimal totalGross, decimal totalPaid, DateTime? dueDate)
    {
        if (totalGross > 0 && totalPaid >= totalGross)
            return InvoiceStatus.Paid;
        if (totalGross < 0 && totalPaid <= totalGross)
            return InvoiceStatus.Paid;
        if (dueDate.HasValue && dueDate.Value < DateTime.UtcNow)
            return InvoiceStatus.Overdue;
        if (totalPaid != 0)
            return InvoiceStatus.PartiallyPaid;
        return InvoiceStatus.Sent;
    }
}
