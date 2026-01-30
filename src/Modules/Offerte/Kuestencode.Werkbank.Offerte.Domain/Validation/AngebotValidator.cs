using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Domain.Validation;

/// <summary>
/// Validierungslogik für Angebote.
/// </summary>
public class AngebotValidator
{
    /// <summary>
    /// Validiert ein Angebot und gibt eine Liste von Fehlermeldungen zurück.
    /// Eine leere Liste bedeutet, dass das Angebot gültig ist.
    /// </summary>
    public IReadOnlyList<string> Validieren(Angebot angebot, bool istNeu = false)
    {
        var fehler = new List<string>();

        // Pflichtfelder
        if (angebot.KundeId <= 0)
        {
            fehler.Add("Ein Kunde muss ausgewählt werden.");
        }

        if (string.IsNullOrWhiteSpace(angebot.Angebotsnummer))
        {
            fehler.Add("Die Angebotsnummer ist erforderlich.");
        }

        // GueltigBis muss in der Zukunft liegen (nur bei Neuanlage)
        if (istNeu && angebot.GueltigBis.Date <= DateTime.UtcNow.Date)
        {
            fehler.Add("Das Gültigkeitsdatum muss in der Zukunft liegen.");
        }

        // Mindestens eine Position erforderlich
        if (angebot.Positionen == null || angebot.Positionen.Count == 0)
        {
            fehler.Add("Das Angebot muss mindestens eine Position enthalten.");
        }
        else
        {
            // Positionen validieren
            for (int i = 0; i < angebot.Positionen.Count; i++)
            {
                var position = angebot.Positionen[i];
                var positionsFehler = ValidierenPosition(position, i + 1);
                fehler.AddRange(positionsFehler);
            }
        }

        return fehler.AsReadOnly();
    }

    /// <summary>
    /// Validiert eine einzelne Angebotsposition.
    /// </summary>
    public IReadOnlyList<string> ValidierenPosition(Angebotsposition position, int positionsNummer)
    {
        var fehler = new List<string>();
        var prefix = $"Position {positionsNummer}: ";

        if (string.IsNullOrWhiteSpace(position.Text))
        {
            fehler.Add($"{prefix}Text ist erforderlich.");
        }

        if (position.Menge <= 0)
        {
            fehler.Add($"{prefix}Menge muss größer als 0 sein.");
        }

        if (position.Einzelpreis < 0)
        {
            fehler.Add($"{prefix}Einzelpreis darf nicht negativ sein.");
        }

        if (position.Steuersatz < 0 || position.Steuersatz > 100)
        {
            fehler.Add($"{prefix}Steuersatz muss zwischen 0 und 100 liegen.");
        }

        if (position.Rabatt.HasValue && (position.Rabatt.Value < 0 || position.Rabatt.Value > 100))
        {
            fehler.Add($"{prefix}Rabatt muss zwischen 0 und 100 Prozent liegen.");
        }

        return fehler.AsReadOnly();
    }

    /// <summary>
    /// Prüft, ob ein Angebot versendet werden kann.
    /// </summary>
    public IReadOnlyList<string> ValidierenFuerVersand(Angebot angebot)
    {
        var fehler = new List<string>(Validieren(angebot, false));

        // Zusätzliche Prüfungen für Versand
        if (angebot.GueltigBis.Date < DateTime.UtcNow.Date)
        {
            fehler.Add("Das Angebot ist bereits abgelaufen und kann nicht versendet werden.");
        }

        if (angebot.Bruttosumme <= 0)
        {
            fehler.Add("Das Angebot muss einen positiven Gesamtbetrag haben.");
        }

        return fehler.AsReadOnly();
    }

    /// <summary>
    /// Prüft, ob das Angebot gültig ist.
    /// </summary>
    public bool IstGueltig(Angebot angebot, bool istNeu = false)
    {
        return Validieren(angebot, istNeu).Count == 0;
    }
}
