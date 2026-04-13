namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

/// <summary>
/// Zuteilung eines Beleg-Teilbetrags zu einem Acta-Projekt.
/// Ein Beleg kann auf mehrere Projekte aufgeteilt werden, wobei die Summe
/// der Teilbeträge den Gesamtbetrag des Belegs nicht überschreiten darf.
/// </summary>
public class DocumentProjectAllocation
{
    public Guid Id { get; set; }

    /// <summary>
    /// Referenz auf den Beleg.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Acta-Projekt-GUID (DeterministicGuid aus ActaProjectDto.Id).
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Zugeteilter Nettobetrag (proportional aus Brutto rückgerechnet).
    /// </summary>
    public decimal AllocatedNet { get; set; }

    /// <summary>
    /// Zugeteilter Steuerbetrag.
    /// </summary>
    public decimal AllocatedTax { get; set; }

    /// <summary>
    /// Zugeteilter Bruttobetrag (Nutzereingabe).
    /// </summary>
    public decimal AllocatedGross { get; set; }

    // Navigation
    public Document Document { get; set; } = null!;
}
