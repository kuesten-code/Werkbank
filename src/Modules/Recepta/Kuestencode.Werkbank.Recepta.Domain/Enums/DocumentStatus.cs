namespace Kuestencode.Werkbank.Recepta.Domain.Enums;

/// <summary>
/// Status eines Belegs im Lebenszyklus.
/// </summary>
public enum DocumentStatus
{
    /// <summary>
    /// Beleg ist im Entwurfsstatus.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Beleg ist verbucht.
    /// </summary>
    Booked = 1,

    /// <summary>
    /// Beleg ist bezahlt.
    /// </summary>
    Paid = 2
}
