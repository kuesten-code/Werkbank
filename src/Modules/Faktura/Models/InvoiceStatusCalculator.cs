namespace Kuestencode.Faktura.Models;

public static class InvoiceStatusCalculator
{
    public static InvoiceStatus Calculate(decimal amountDue, decimal totalPaid, DateTime? dueDate)
    {
        if (amountDue > 0 && totalPaid >= amountDue)
            return InvoiceStatus.Paid;
        if (amountDue < 0 && totalPaid <= amountDue)
            return InvoiceStatus.Paid;
        if (dueDate.HasValue && dueDate.Value < DateTime.UtcNow)
            return InvoiceStatus.Overdue;
        if (totalPaid != 0)
            return InvoiceStatus.PartiallyPaid;
        return InvoiceStatus.Sent;
    }
}
