namespace Kuestencode.Shared.UI.Components.Settings;

/// <summary>
/// Kontext für die Art des Dokuments (Rechnung, Angebot, etc.).
/// Definiert die Labels und Platzhalter.
/// </summary>
public class DocumentSettingsContext
{
    public required string DocumentTypeName { get; init; }
    public required string DocumentTypeNamePlural { get; init; }
    public required string PageTitleSuffix { get; init; }

    /// <summary>
    /// Vollständige Beschreibung für den Einleitungssatz im Email-Einstellungs-Editor,
    /// z.B. "Ihrer Rechnungs-E-Mails" oder generisch "Ihrer E-Mails".
    /// </summary>
    public required string EmailDescription { get; init; }

    // Platzhalter für E-Mail
    public required List<PlaceholderInfo> EmailPlaceholders { get; init; }

    // Platzhalter für PDF
    public required List<PlaceholderInfo> PdfPlaceholders { get; init; }

    // Label für das Notice-Feld (Zahlungshinweis vs Gültigkeitshinweis)
    public required string NoticeFieldLabel { get; init; }
    public required string NoticeFieldPlaceholder { get; init; }

    public static DocumentSettingsContext Rechnung => new()
    {
        DocumentTypeName = "Rechnung",
        DocumentTypeNamePlural = "Rechnungen",
        PageTitleSuffix = "Küstencode Faktura",
        EmailDescription = "Ihrer Rechnungs-E-Mails",
        NoticeFieldLabel = "Zahlungshinweis",
        NoticeFieldPlaceholder = "Bitte überweisen Sie den Betrag bis zum {{Faelligkeitsdatum}} auf unser Konto.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname")
        },
        PdfPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Rechnungsnummer}}", "Rechnungsnummer"),
            new("{{Rechnungsdatum}}", "Rechnungsdatum"),
            new("{{Faelligkeitsdatum}}", "Fälligkeitsdatum"),
            new("{{Rechnungsbetrag}}", "Rechnungsbetrag"),
            new("{{Kundenname}}", "Kundenname")
        }
    };

    public static DocumentSettingsContext Angebot => new()
    {
        DocumentTypeName = "Angebot",
        DocumentTypeNamePlural = "Angebote",
        PageTitleSuffix = "Küstencode Offerte",
        EmailDescription = "Ihrer Angebots-E-Mails",
        NoticeFieldLabel = "Gültigkeitshinweis",
        NoticeFieldPlaceholder = "Dieses Angebot ist gültig bis zum {{Gueltigkeitsdatum}}.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname")
        },
        PdfPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Angebotsnummer}}", "Angebotsnummer"),
            new("{{Angebotsdatum}}", "Angebotsdatum"),
            new("{{Gueltigkeitsdatum}}", "Gültigkeitsdatum"),
            new("{{Angebotsbetrag}}", "Angebotsbetrag"),
            new("{{Kundenname}}", "Kundenname")
        }
    };

    public static DocumentSettingsContext Taetigkeit => new()
    {
        DocumentTypeName = "Tätigkeitsnachweis",
        DocumentTypeNamePlural = "Tätigkeitsnachweise",
        PageTitleSuffix = "Küstencode Rapport",
        EmailDescription = "Ihrer Tätigkeitsnachweis-E-Mails",
        NoticeFieldLabel = "Hinweistext",
        NoticeFieldPlaceholder = "Optionaler Hinweistext für den Tätigkeitsnachweis.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname")
        },
        PdfPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Kundenname}}", "Kundenname"),
            new("{{Zeitraum}}", "Berichtszeitraum"),
            new("{{Gesamtstunden}}", "Summe der Stunden")
        }
    };

    /// <summary>
    /// Generischer Kontext für die modulübergreifende Email-Design-Seite in Host —
    /// gilt für alle E-Mail-Arten (Rechnung, Angebot, Tätigkeitsnachweis, ...) gleichermaßen.
    /// </summary>
    public static DocumentSettingsContext Allgemein => new()
    {
        DocumentTypeName = "Dokument",
        DocumentTypeNamePlural = "Dokumente",
        PageTitleSuffix = "Küstencode Werkbank",
        EmailDescription = "Ihrer E-Mails",
        NoticeFieldLabel = "Hinweistext",
        NoticeFieldPlaceholder = "Optionaler Hinweistext.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname")
        },
        PdfPlaceholders = new List<PlaceholderInfo>()
    };
}

public record PlaceholderInfo(string Placeholder, string Description);
