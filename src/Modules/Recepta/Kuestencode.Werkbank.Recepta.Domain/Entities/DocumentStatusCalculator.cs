using Kuestencode.Werkbank.Recepta.Domain.Enums;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

public static class DocumentStatusCalculator
{
    public static DocumentStatus Calculate(decimal amountGross, decimal totalPaid, decimal skontoAmount = 0m)
    {
        var threshold = amountGross >= 0 ? amountGross - skontoAmount : amountGross + skontoAmount;
        var settled = amountGross >= 0 ? totalPaid >= threshold : totalPaid <= threshold;
        if (amountGross != 0 && settled) return DocumentStatus.Paid;
        var hasPayment = amountGross >= 0 ? totalPaid > 0 : totalPaid < 0;
        if (hasPayment) return DocumentStatus.PartiallyPaid;
        return DocumentStatus.Booked;
    }
}
