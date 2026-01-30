using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;

namespace Kuestencode.Werkbank.Offerte.Domain.Services;

/// <summary>
/// Service zur Verwaltung der Statusübergänge eines Angebots.
/// Implementiert die State Machine für den Angebots-Lebenszyklus.
///
/// Erlaubte Übergänge:
/// - Entwurf → Versendet
/// - Versendet → Angenommen
/// - Versendet → Abgelehnt
/// - Versendet → Abgelaufen
///
/// Terminal-Status: Angenommen, Abgelehnt, Abgelaufen
/// </summary>
public class AngebotStatusService
{
    /// <summary>
    /// Prüft, ob ein Angebot versendet werden kann.
    /// Voraussetzung: Status ist Entwurf.
    /// </summary>
    public bool KannVersendetWerden(Angebot angebot)
    {
        return angebot.Status == AngebotStatus.Entwurf;
    }

    /// <summary>
    /// Prüft, ob ein Angebot angenommen werden kann.
    /// Voraussetzung: Status ist Versendet und nicht abgelaufen.
    /// </summary>
    public bool KannAngenommenWerden(Angebot angebot)
    {
        return angebot.Status == AngebotStatus.Versendet && !IstAbgelaufen(angebot);
    }

    /// <summary>
    /// Prüft, ob ein Angebot abgelehnt werden kann.
    /// Voraussetzung: Status ist Versendet.
    /// </summary>
    public bool KannAbgelehntWerden(Angebot angebot)
    {
        return angebot.Status == AngebotStatus.Versendet;
    }

    /// <summary>
    /// Prüft, ob ein Angebot als abgelaufen markiert werden kann.
    /// Voraussetzung: Status ist Versendet und GueltigBis ist überschritten.
    /// </summary>
    public bool KannAlsAbgelaufenMarkiertWerden(Angebot angebot)
    {
        return angebot.Status == AngebotStatus.Versendet && IstAbgelaufen(angebot);
    }

    /// <summary>
    /// Prüft, ob das Gültigkeitsdatum überschritten ist.
    /// </summary>
    public bool IstAbgelaufen(Angebot angebot)
    {
        return DateTime.UtcNow.Date > angebot.GueltigBis.Date;
    }

    /// <summary>
    /// Prüft, ob ein Angebot kopiert werden kann.
    /// Alle Angebote können kopiert werden.
    /// </summary>
    public bool KannKopiertWerden(Angebot angebot)
    {
        return true;
    }

    /// <summary>
    /// Prüft, ob ein Angebot bearbeitet werden kann.
    /// Voraussetzung: Status ist Entwurf.
    /// </summary>
    public bool KannBearbeitetWerden(Angebot angebot)
    {
        return angebot.Status == AngebotStatus.Entwurf;
    }

    /// <summary>
    /// Versetzt das Angebot in den Status "Versendet".
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Versenden(Angebot angebot)
    {
        if (!KannVersendetWerden(angebot))
        {
            throw new InvalidOperationException(
                $"Angebot kann nicht versendet werden. Aktueller Status: {angebot.Status}");
        }

        angebot.Status = AngebotStatus.Versendet;
        angebot.VersendetAm = DateTime.UtcNow;
    }

    /// <summary>
    /// Versetzt das Angebot in den Status "Angenommen".
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Annehmen(Angebot angebot)
    {
        if (!KannAngenommenWerden(angebot))
        {
            throw new InvalidOperationException(
                $"Angebot kann nicht angenommen werden. Aktueller Status: {angebot.Status}, Abgelaufen: {IstAbgelaufen(angebot)}");
        }

        angebot.Status = AngebotStatus.Angenommen;
        angebot.AngenommenAm = DateTime.UtcNow;
    }

    /// <summary>
    /// Versetzt das Angebot in den Status "Abgelehnt".
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Ablehnen(Angebot angebot)
    {
        if (!KannAbgelehntWerden(angebot))
        {
            throw new InvalidOperationException(
                $"Angebot kann nicht abgelehnt werden. Aktueller Status: {angebot.Status}");
        }

        angebot.Status = AngebotStatus.Abgelehnt;
        angebot.AbgelehntAm = DateTime.UtcNow;
    }

    /// <summary>
    /// Versetzt das Angebot in den Status "Abgelaufen".
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void AlsAbgelaufenMarkieren(Angebot angebot)
    {
        if (!KannAlsAbgelaufenMarkiertWerden(angebot))
        {
            throw new InvalidOperationException(
                $"Angebot kann nicht als abgelaufen markiert werden. Aktueller Status: {angebot.Status}, GueltigBis: {angebot.GueltigBis}");
        }

        angebot.Status = AngebotStatus.Abgelaufen;
        angebot.AbgelaufenAm = DateTime.UtcNow;
    }

    /// <summary>
    /// Gibt die erlaubten Folgestatus für ein Angebot zurück.
    /// </summary>
    public IEnumerable<AngebotStatus> GetErlaubteUebergaenge(Angebot angebot)
    {
        var erlaubt = new List<AngebotStatus>();

        if (KannVersendetWerden(angebot))
            erlaubt.Add(AngebotStatus.Versendet);

        if (KannAngenommenWerden(angebot))
            erlaubt.Add(AngebotStatus.Angenommen);

        if (KannAbgelehntWerden(angebot))
            erlaubt.Add(AngebotStatus.Abgelehnt);

        if (KannAlsAbgelaufenMarkiertWerden(angebot))
            erlaubt.Add(AngebotStatus.Abgelaufen);

        return erlaubt;
    }
}
