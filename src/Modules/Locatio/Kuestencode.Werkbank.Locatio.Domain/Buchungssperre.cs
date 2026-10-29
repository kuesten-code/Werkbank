using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;

namespace Kuestencode.Werkbank.Locatio.Domain;

/// <summary>
/// Ab dem Verkaufsdatum und für Fremdeigentum dürfen keine Einnahmen, Kosten oder AfA mehr entstehen.
/// </summary>
public static class Buchungssperre
{
    public static void Pruefe(Mieteinheit einheit, DateOnly datum)
    {
        if (einheit.Status == EinheitStatus.Fremdeigentum)
            throw new InvalidOperationException(
                $"Einheit {einheit.Bezeichnung} ist Fremdeigentum, Buchungen sind nicht möglich.");

        if (einheit.GesperrtAb is { } gesperrtAb && datum >= gesperrtAb)
            throw new InvalidOperationException(
                $"Einheit {einheit.Bezeichnung} ist seit {gesperrtAb:dd.MM.yyyy} veräußert, Buchungen ab diesem Datum sind gesperrt.");
    }

    public static void Pruefe(Gebaeude gebaeude, DateOnly datum)
    {
        if (gebaeude.VerkaufsDatum is { } verkauf && datum >= verkauf)
            throw new InvalidOperationException(
                $"Gebäude {gebaeude.Name} ist seit {verkauf:dd.MM.yyyy} veräußert, Buchungen ab diesem Datum sind gesperrt.");
    }
}
