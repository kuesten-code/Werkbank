namespace Kuestencode.Werkbank.Offerte.Domain.Enums;

/// <summary>
/// Status eines Angebots im Lebenszyklus.
/// </summary>
public enum AngebotStatus
{
    /// <summary>
    /// Angebot ist in Bearbeitung und noch nicht versendet.
    /// </summary>
    Entwurf = 0,

    /// <summary>
    /// Angebot wurde an den Kunden versendet.
    /// </summary>
    Versendet = 1,

    /// <summary>
    /// Kunde hat das Angebot angenommen (Endstatus).
    /// </summary>
    Angenommen = 2,

    /// <summary>
    /// Kunde hat das Angebot abgelehnt (Endstatus).
    /// </summary>
    Abgelehnt = 3,

    /// <summary>
    /// Angebot ist abgelaufen, da GueltigBis überschritten (Endstatus).
    /// </summary>
    Abgelaufen = 4
}
