namespace Kuestencode.Shared.Pdf.Core;

/// <summary>
/// Zusammenfassung der Beträge eines Dokuments.
/// </summary>
public class DocumentSummary
{
    /// <summary>Nettosumme aller Positionen</summary>
    public decimal TotalNet { get; init; }

    /// <summary>Gesamtrabatt (falls vorhanden)</summary>
    public decimal? DiscountAmount { get; init; }

    /// <summary>Rabatt in Prozent (für Anzeige)</summary>
    public decimal? DiscountPercent { get; init; }

    /// <summary>Nettosumme nach Rabatt</summary>
    public decimal TotalNetAfterDiscount => TotalNet - (DiscountAmount ?? 0);

    /// <summary>MwSt-Beträge gruppiert nach Steuersatz</summary>
    public List<VatGroup> VatGroups { get; init; } = new();

    /// <summary>Gesamte MwSt</summary>
    public decimal TotalVat => VatGroups.Sum(g => g.Amount);

    /// <summary>Bruttosumme</summary>
    public decimal TotalGross => TotalNetAfterDiscount + TotalVat;

    /// <summary>Anzahlungen (optional, nur bei Rechnungen)</summary>
    public List<DownPaymentInfo>? DownPayments { get; init; }

    /// <summary>Gesamt Anzahlungen</summary>
    public decimal TotalDownPayments => DownPayments?.Sum(d => d.Amount) ?? 0;

    /// <summary>Zu zahlender Betrag</summary>
    public decimal AmountDue => TotalGross - TotalDownPayments;
}

/// <summary>
/// Gruppierung der MwSt nach Steuersatz.
/// </summary>
public class VatGroup
{
    /// <summary>Steuersatz in Prozent</summary>
    public decimal Rate { get; init; }

    /// <summary>Steuerbetrag</summary>
    public decimal Amount { get; init; }
}

/// <summary>
/// Information über eine Anzahlung.
/// </summary>
public class DownPaymentInfo
{
    /// <summary>Beschreibung der Anzahlung</summary>
    public required string Description { get; init; }

    /// <summary>Betrag</summary>
    public decimal Amount { get; init; }

    /// <summary>Zahlungsdatum (optional)</summary>
    public DateTime? PaymentDate { get; init; }
}
