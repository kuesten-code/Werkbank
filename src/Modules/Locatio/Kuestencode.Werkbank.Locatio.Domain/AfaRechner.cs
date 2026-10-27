using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;

namespace Kuestencode.Werkbank.Locatio.Domain;

/// <summary>
/// Lineare Gebäude-AfA je Einheit, im Anschaffungs- und Verkaufsjahr monatsgenau (§ 7 Abs. 1 Satz 4 EStG).
/// Das Eigentum endet am Tag vor dem Verkaufsdatum, passend zur <see cref="Buchungssperre"/>.
/// </summary>
public static class AfaRechner
{
    public static (int Monate, decimal Betrag, DateOnly Buchungsdatum) Berechne(
        Mieteinheit einheit, int jahr, decimal bereitsAngesetzt)
    {
        var jahresende = new DateOnly(jahr, 12, 31);
        if (einheit.Status == EinheitStatus.Fremdeigentum || einheit.AnschaffungsDatum is not { } beginn || einheit.AfaBasis <= 0)
            return (0, 0m, jahresende);

        var ende = einheit.GesperrtAb?.AddDays(-1) ?? jahresende;
        if (beginn.Year > jahr || ende.Year < jahr || ende < beginn)
            return (0, 0m, jahresende);

        var ersterMonat = beginn.Year == jahr ? beginn.Month : 1;
        var letzterMonat = ende.Year == jahr ? ende.Month : 12;
        var monate = letzterMonat - ersterMonat + 1;

        var jahresAfa = einheit.AfaBasis * einheit.AfaSatz / 100m;
        var betrag = Math.Round(jahresAfa * monate / 12m, 2);
        var restwert = Math.Max(0m, einheit.AfaBasis - bereitsAngesetzt);

        return (monate, Math.Min(betrag, restwert), ende < jahresende ? ende : jahresende);
    }
}
