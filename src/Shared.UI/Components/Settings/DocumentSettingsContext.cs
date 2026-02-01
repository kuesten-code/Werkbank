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
        NoticeFieldLabel = "Zahlungshinweis",
        NoticeFieldPlaceholder = "Bitte überweisen Sie den Betrag bis zum {{Faelligkeitsdatum}} auf unser Konto.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Rechnungsnummer}}", "Rechnungsnummer"),
            new("{{Faelligkeitsdatum}}", "Fälligkeitsdatum")
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
        NoticeFieldLabel = "Gültigkeitshinweis",
        NoticeFieldPlaceholder = "Dieses Angebot ist gültig bis zum {{Gueltigkeitsdatum}}.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Angebotsnummer}}", "Angebotsnummer"),
            new("{{Gueltigkeitsdatum}}", "Gültigkeitsdatum")
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
        NoticeFieldLabel = "Hinweistext",
        NoticeFieldPlaceholder = "Optionaler Hinweistext für den Tätigkeitsnachweis.",
        EmailPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Kundenname}}", "Kundenname"),
            new("{{Zeitraum}}", "Berichtszeitraum")
        },
        PdfPlaceholders = new List<PlaceholderInfo>
        {
            new("{{Firmenname}}", "Ihr Firmenname"),
            new("{{Kundenname}}", "Kundenname"),
            new("{{Zeitraum}}", "Berichtszeitraum"),
            new("{{Gesamtstunden}}", "Summe der Stunden")
        }
    };
}

public record PlaceholderInfo(string Placeholder, string Description);
