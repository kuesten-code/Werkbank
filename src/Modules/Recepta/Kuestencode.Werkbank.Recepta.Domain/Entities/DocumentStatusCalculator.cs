using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

public static class DocumentStatusCalculator
{
    public static DocumentStatus Calculate(decimal amountGross, decimal totalPaid, decimal skontoAmount = 0m)
    {
        var threshold = amountGross - skontoAmount;
        if (amountGross > 0 && totalPaid >= threshold) return DocumentStatus.Paid;
        if (totalPaid > 0) return DocumentStatus.PartiallyPaid;
        return DocumentStatus.Booked;
    }
}
