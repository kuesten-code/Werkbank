namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Gemeinsame Anschaffungs- und Veräußerungsdaten für Gebäude und Mieteinheiten.
/// Alle Werte sind optional; die Anschaffungskosten sind zugleich die AfA-Basis.
/// </summary>
public abstract class Eigentumsobjekt
{
    public DateOnly? AnschaffungsDatum { get; set; }
    public decimal? AnschaffungskostenGebaeude { get; set; }
    public decimal? AnschaffungskostenGrundUndBoden { get; set; }
    public decimal? Anschaffungsnebenkosten { get; set; }

    public DateOnly? VerkaufsDatum { get; set; }
    public decimal? Veraeusserungspreis { get; set; }
    public decimal? Veraeusserungskosten { get; set; }

    /// <summary>Linearer AfA-Satz in Prozent pro Jahr.</summary>
    public decimal AfaSatz { get; set; } = 2m;

    public decimal AnschaffungskostenGesamt =>
        (AnschaffungskostenGebaeude ?? 0) + (AnschaffungskostenGrundUndBoden ?? 0) + (Anschaffungsnebenkosten ?? 0);

    /// <summary>
    /// Grund und Boden nutzt sich nicht ab. Die Nebenkosten werden deshalb im Verhältnis
    /// Gebäude zu Grund und Boden aufgeteilt und nur der Gebäudeanteil abgeschrieben.
    /// </summary>
    public decimal AfaBasis
    {
        get
        {
            var gebaeude = AnschaffungskostenGebaeude ?? 0;
            var kaufpreis = gebaeude + (AnschaffungskostenGrundUndBoden ?? 0);
            if (gebaeude <= 0)
                return 0;

            var nebenkostenAnteil = (Anschaffungsnebenkosten ?? 0) * gebaeude / kaufpreis;
            return Math.Round(gebaeude + nebenkostenAnteil, 2);
        }
    }
}
