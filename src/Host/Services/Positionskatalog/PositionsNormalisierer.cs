using System.Text;

namespace Kuestencode.Werkbank.Host.Services.Positionskatalog;

public static class PositionsNormalisierer
{
    private static readonly HashSet<string> StundenEinheiten = new(StringComparer.OrdinalIgnoreCase)
    {
        "h", "std", "std.", "stunde", "stunden"
    };

    public static string Schluessel(string text, string? einheit)
        => $"{Vereinheitlichen(text)}|{Vereinheitlichen(einheit)}";

    public static bool IstStundenEinheit(string? einheit)
        => einheit != null && StundenEinheiten.Contains(einheit.Trim());

    /// <summary>
    /// Zerlegt einen Text in vergleichbare Wörter: Umlaute ausgeschrieben, Satzzeichen als Trenner.
    /// </summary>
    public static string[] WoerterFuerSuche(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var sb = new StringBuilder(text.Length + 8);
        foreach (var c in text.ToLowerInvariant())
        {
            switch (c)
            {
                case 'ä': sb.Append("ae"); break;
                case 'ö': sb.Append("oe"); break;
                case 'ü': sb.Append("ue"); break;
                case 'ß': sb.Append("ss"); break;
                default: sb.Append(char.IsLetterOrDigit(c) ? c : ' '); break;
            }
        }

        return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string Vereinheitlichen(string? wert)
        => string.Join(' ', (wert ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
}
