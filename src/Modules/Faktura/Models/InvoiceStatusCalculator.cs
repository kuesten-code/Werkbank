namespace Kuestencode.Faktura.Models;

public static class InvoiceStatusCalculator
{
    public static InvoiceStatus Calculate(decimal totalGross, decimal totalPaid, DateTime? dueDate)
    {
        if (totalGross > 0 && totalPaid >= totalGross)
            return InvoiceStatus.Paid;
        if (totalGross < 0 && totalPaid <= totalGross)
            return InvoiceStatus.Paid;
        if (totalPaid != 0)
            return InvoiceStatus.PartiallyPaid;
        if (dueDate.HasValue && dueDate.Value < DateTime.UtcNow)
            return InvoiceStatus.Overdue;
        return InvoiceStatus.Sent;
    }
}
