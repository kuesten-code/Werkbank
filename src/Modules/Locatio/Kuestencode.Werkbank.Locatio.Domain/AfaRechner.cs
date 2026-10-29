using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;

namespace Kuestencode.Werkbank.Locatio.Domain;

/// <summary>
/// Anschaffungsdaten, aus denen die AfA einer Einheit abgeleitet wird: die eigenen (Einzelerwerb)
/// oder der Flächenanteil am Gebäude (Erwerb des ganzen Hauses mit einem Kaufpreis).
/// </summary>
public record AfaGrundlage(Eigentumsobjekt Quelle, decimal Anteil)
{
    public DateOnly? AnschaffungsDatum => Quelle.AnschaffungsDatum;
    public decimal AfaSatz => Quelle.AfaSatz;
    public decimal AfaBasis => Math.Round(Quelle.AfaBasis * Anteil, 2);
    public decimal Anschaffungskosten => Math.Round(Quelle.AnschaffungskostenGesamt * Anteil, 2);
    public bool AusGebaeude => Quelle is Gebaeude;
}

/// <summary>
/// Lineare Gebäude-AfA je Einheit, im Anschaffungs- und Verkaufsjahr monatsgenau (§ 7 Abs. 1 Satz 4 EStG).
/// Jede Wohnung ist ein eigenes Wirtschaftsgut; nur so lassen sich Einzelverkauf und unterschiedliche Nutzung abbilden.
/// Das Eigentum endet am Tag vor dem Verkaufsdatum, passend zur <see cref="Buchungssperre"/>.
/// </summary>
public static class AfaRechner
{
    /// <summary>
    /// Eigene Anschaffungsdaten haben Vorrang. Sonst wird das Gebäude nach Fläche auf alle eigenen Einheiten
    /// ohne eigene Anschaffungsdaten verteilt – auch veräußerte, da sie mit dem Gebäude angeschafft wurden.
    /// Fehlt einer dieser Einheiten die Fläche, ist keine Verteilung möglich.
    /// Erwartet <see cref="Mieteinheit.Gebaeude"/> samt Einheiten.
    /// </summary>
    public static AfaGrundlage? Grundlage(Mieteinheit einheit)
    {
        if (einheit.Status == EinheitStatus.Fremdeigentum)
            return null;
        if (HatEigeneAnschaffung(einheit))
            return new AfaGrundlage(einheit, 1m);
        if (einheit.Gebaeude is not { AnschaffungskostenGesamt: > 0 } gebaeude)
            return null;

        var beteiligte = AnGebaeudeBeteiligt(gebaeude);
        if (beteiligte.Any(e => e.Flaeche is not > 0))
            return null;

        return new AfaGrundlage(gebaeude, einheit.Flaeche!.Value / beteiligte.Sum(e => e.Flaeche!.Value));
    }

    /// <summary>Einheiten, auf die die Anschaffungskosten des Gebäudes nach Fläche verteilt werden.</summary>
    public static List<Mieteinheit> AnGebaeudeBeteiligt(Gebaeude gebaeude) =>
        gebaeude.Einheiten.Where(e => e.Status != EinheitStatus.Fremdeigentum && !HatEigeneAnschaffung(e)).ToList();

    public static (int Monate, decimal Betrag, DateOnly Buchungsdatum) Berechne(
        Mieteinheit einheit, int jahr, decimal bereitsAngesetzt)
    {
        var jahresende = new DateOnly(jahr, 12, 31);
        if (Grundlage(einheit) is not { AnschaffungsDatum: { } beginn, AfaBasis: > 0 } grundlage)
            return (0, 0m, jahresende);

        var ende = einheit.GesperrtAb?.AddDays(-1) ?? jahresende;
        if (beginn.Year > jahr || ende.Year < jahr || ende < beginn)
            return (0, 0m, jahresende);

        var ersterMonat = beginn.Year == jahr ? beginn.Month : 1;
        var letzterMonat = ende.Year == jahr ? ende.Month : 12;
        var monate = letzterMonat - ersterMonat + 1;

        var jahresAfa = grundlage.AfaBasis * grundlage.AfaSatz / 100m;
        var betrag = Math.Round(jahresAfa * monate / 12m, 2);
        var restwert = Math.Max(0m, grundlage.AfaBasis - bereitsAngesetzt);

        return (monate, Math.Min(betrag, restwert), ende < jahresende ? ende : jahresende);
    }

    private static bool HatEigeneAnschaffung(Mieteinheit einheit) => einheit.AnschaffungskostenGesamt > 0;
}
