namespace Kuestencode.Shared.Pdf.Core;

/// <summary>
/// Einzelne Position in einem Dokument (Rechnungsposition, Angebotsposition).
/// </summary>
public class DocumentLineItem
{
    /// <summary>Positionsnummer</summary>
    public int Position { get; init; }

    /// <summary>Beschreibung der Position</summary>
    public required string Description { get; init; }

    /// <summary>Menge</summary>
    public decimal Quantity { get; init; }

    /// <summary>Einzelpreis (Netto)</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Steuersatz in Prozent</summary>
    public decimal VatRate { get; init; }

    /// <summary>Rabatt in Prozent (optional)</summary>
    public decimal? DiscountPercent { get; init; }

    /// <summary>Nettosumme der Position (berechnet)</summary>
    public decimal TotalNet => CalculateTotalNet();

    /// <summary>Steuerbetrag der Position (berechnet)</summary>
    public decimal VatAmount => TotalNet * VatRate / 100;

    private decimal CalculateTotalNet()
    {
        var baseAmount = Quantity * UnitPrice;
        if (DiscountPercent.HasValue && DiscountPercent.Value > 0)
        {
            baseAmount -= baseAmount * DiscountPercent.Value / 100;
        }
        return baseAmount;
    }
}
