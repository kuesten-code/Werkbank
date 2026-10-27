namespace Kuestencode.Werkbank.Locatio.Domain.Enums;

/// <summary>
/// Steuert, wie Buchungen einer Kategorie an Saldo übergeben werden.
/// </summary>
public enum KategorieTyp
{
    Werbungskosten = 0,
    Darlehenszinsen = 1,

    /// <summary>Nicht abziehbar, wird nie an Saldo übergeben.</summary>
    Tilgung = 2,

    Afa = 3,
    Mieteinnahme = 4,
    Nebenkostenabrechnung = 5
}

public static class KategorieTypExtensions
{
    /// <summary>Typen, die als Kostenposten erfasst werden können.</summary>
    public static readonly KategorieTyp[] Ausgabetypen =
        [KategorieTyp.Werbungskosten, KategorieTyp.Darlehenszinsen, KategorieTyp.Tilgung, KategorieTyp.Afa];

    public static string Anzeigename(this KategorieTyp typ) => typ switch
    {
        KategorieTyp.Werbungskosten => "Werbungskosten",
        KategorieTyp.Darlehenszinsen => "Darlehenszinsen",
        KategorieTyp.Tilgung => "Tilgung (nicht abziehbar)",
        KategorieTyp.Afa => "Abschreibung (AfA)",
        KategorieTyp.Mieteinnahme => "Mieteinnahme",
        KategorieTyp.Nebenkostenabrechnung => "Nebenkostenabrechnung",
        _ => typ.ToString()
    };
}
