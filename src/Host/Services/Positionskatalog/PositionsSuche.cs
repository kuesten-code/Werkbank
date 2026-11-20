using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services.Positionskatalog;

/// <summary>
/// Tippfehlertolerante Suche: Jedes Suchwort muss zum Anfang eines Wortes der Vorlage passen,
/// exakt oder mit wenigen Tippfehlern (Damerau-Levenshtein).
/// </summary>
public static class PositionsSuche
{
    private const int PraefixWertung = 100;
    private const int FehlerAbzug = 30;

    public static List<Positionsvorlage> Suchen(IEnumerable<Positionsvorlage> vorlagen, string? suche, int limit)
    {
        var suchWoerter = PositionsNormalisierer.WoerterFuerSuche(suche);
        if (suchWoerter.Length == 0)
        {
            return vorlagen
                .OrderByDescending(v => v.ZuletztVerwendetAm)
                .Take(limit)
                .ToList();
        }

        return vorlagen
            .Select(v => (Vorlage: v, Wertung: Bewerten(suchWoerter, PositionsNormalisierer.WoerterFuerSuche(v.Text))))
            .Where(t => t.Wertung > 0)
            .OrderByDescending(t => t.Wertung)
            .ThenByDescending(t => t.Vorlage.ZuletztVerwendetAm)
            .Take(limit)
            .Select(t => t.Vorlage)
            .ToList();
    }

    private static int Bewerten(string[] suchWoerter, string[] vorlagenWoerter)
    {
        var summe = 0;
        foreach (var suchWort in suchWoerter)
        {
            var beste = vorlagenWoerter.Max(w => (int?)WortWertung(suchWort, w)) ?? 0;
            if (beste == 0)
                return 0;
            summe += beste;
        }
        return summe;
    }

    public static int WortWertung(string suchWort, string vorlagenWort)
    {
        if (vorlagenWort.StartsWith(suchWort, StringComparison.Ordinal))
            return PraefixWertung;

        var erlaubt = ErlaubteFehler(suchWort.Length);
        if (erlaubt == 0)
            return 0;

        // Das Präfix darf um die erlaubten Fehler länger oder kürzer sein (fehlender/zusätzlicher Buchstabe).
        var beste = int.MaxValue;
        for (var laenge = suchWort.Length - erlaubt; laenge <= suchWort.Length + erlaubt; laenge++)
        {
            if (laenge < 1 || laenge > vorlagenWort.Length)
                continue;
            beste = Math.Min(beste, Distanz(suchWort, vorlagenWort[..laenge]));
        }

        return beste <= erlaubt ? PraefixWertung - beste * FehlerAbzug : 0;
    }

    public static int ErlaubteFehler(int laenge) => laenge switch
    {
        < 4 => 0,
        < 8 => 1,
        _ => 2
    };

    public static int Distanz(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var kosten = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + kosten);

                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }

        return d[a.Length, b.Length];
    }
}
