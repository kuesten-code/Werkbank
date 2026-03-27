namespace Kuestencode.Werkbank.Recepta.Domain.Enums;

/// <summary>
/// Kategorie eines Belegs – EÜR-konform gegliedert.
/// Gespeichert als String (Enum-Name), daher sind die Integer-Werte nur zur Dokumentation.
/// </summary>
public enum DocumentCategory
{
    // Wareneinkauf
    Material = 0,

    // Fremdleistungen
    Subcontractor = 1,

    // Personalkosten
    Wages = 10,
    SocialSecurity = 11,

    // Raumkosten
    Rent = 20,
    Utilities = 21,

    // Fahrzeugkosten
    Vehicle = 30,

    // Reisekosten
    Travel = 40,

    // Werbung
    Marketing = 50,

    // Büro & IT
    Office = 60,
    Software = 61,
    Phone = 62,

    // Versicherungen & Beiträge
    Insurance = 70,
    Fees = 71,

    // Sonstiges
    Other = 99
}
