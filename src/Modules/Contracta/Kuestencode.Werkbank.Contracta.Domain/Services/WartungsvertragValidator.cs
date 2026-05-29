using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Kuestencode.Werkbank.Contracta.Domain.Enums;

namespace Kuestencode.Werkbank.Contracta.Domain.Services;

public class WartungsvertragValidator
{
    public IReadOnlyList<string> Validiere(Wartungsvertrag vertrag)
    {
        var fehler = new List<string>();

        if (vertrag.Positionen.Count == 0)
            fehler.Add("Der Vertrag muss mindestens eine Position enthalten.");

        if (vertrag.Startdatum == default)
            fehler.Add("Das Startdatum ist erforderlich.");

        if (vertrag.Intervall == Abrechnungsintervall.Custom &&
            (vertrag.CustomIntervallTage is null or <= 0))
            fehler.Add("Bei benutzerdefiniertem Intervall muss die Anzahl der Tage größer als 0 sein.");

        if (vertrag.Enddatum.HasValue && vertrag.Enddatum <= vertrag.Startdatum)
            fehler.Add("Das Enddatum muss nach dem Startdatum liegen.");

        return fehler;
    }

    public bool IstGueltig(Wartungsvertrag vertrag) => Validiere(vertrag).Count == 0;
}
