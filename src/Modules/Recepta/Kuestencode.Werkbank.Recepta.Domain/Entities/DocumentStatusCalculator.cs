using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

public static class DocumentStatusCalculator
{
    public static DocumentStatus Calculate(decimal amountGross, decimal totalPaid)
    {
        if (amountGross > 0 && totalPaid >= amountGross) return DocumentStatus.Paid;
        if (totalPaid > 0) return DocumentStatus.PartiallyPaid;
        return DocumentStatus.Booked;
    }
}
