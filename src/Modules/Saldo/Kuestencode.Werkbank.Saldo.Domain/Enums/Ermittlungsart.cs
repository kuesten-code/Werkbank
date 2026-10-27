namespace Kuestencode.Werkbank.Saldo.Domain.Enums;

/// <summary>
/// Art der Überschussermittlung. Die Rechenlogik ist identisch (Zufluss-/Abflussprinzip),
/// nur die steuerlichen Begriffe unterscheiden sich.
/// </summary>
public enum Ermittlungsart
{
    /// <summary>Einnahmen-Überschuss-Rechnung, § 4 Abs. 3 EStG (Gewinneinkünfte).</summary>
    Euer = 0,

    /// <summary>Überschuss der Einnahmen über die Werbungskosten, § 21 EStG (Vermietung und Verpachtung).</summary>
    VermietungUndVerpachtung = 1
}

public record ErmittlungsBegriffe(
    string Titel,
    string Kurz,
    string Einnahmen,
    string Ausgaben,
    string Ueberschuss,
    string Fehlbetrag)
{
    public static ErmittlungsBegriffe Fuer(Ermittlungsart art) => art switch
    {
        Ermittlungsart.VermietungUndVerpachtung => new(
            "Überschussrechnung Vermietung und Verpachtung", "V+V",
            "Einnahmen", "Werbungskosten", "Überschuss", "Werbungskostenüberschuss"),
        _ => new(
            "Einnahmen-Überschuss-Rechnung", "EÜR",
            "Betriebseinnahmen", "Betriebsausgaben", "Gewinn (Überschuss)", "Verlust")
    };

    public string Ergebnis(decimal betrag) => betrag >= 0 ? Ueberschuss : Fehlbetrag;
}
