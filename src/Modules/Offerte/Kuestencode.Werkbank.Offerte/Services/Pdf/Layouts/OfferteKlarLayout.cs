using Kuestencode.Core.Models;
using Kuestencode.Shared.Pdf.Core;
using Kuestencode.Shared.Pdf.Layouts;
using Kuestencode.Shared.Pdf.Styling;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Kuestencode.Werkbank.Offerte.Services.Pdf.Layouts;

/// <summary>
/// "Klar" Layout-Implementierung für Angebots-PDFs.
/// Erbt vom Shared KlarDocumentLayout.
/// </summary>
public class OfferteKlarLayout : KlarDocumentLayout, IDocument
{
    private readonly Angebot _angebot;
    private readonly Customer _kunde;
    private readonly Company _firma;
    private readonly OfferteSettings _settings;

    public OfferteKlarLayout(Angebot angebot, Customer kunde, Company firma, OfferteSettings settings)
    {
        _angebot = angebot;
        _kunde = kunde;
        _firma = firma;
        _settings = settings;
    }

    protected override PdfDocumentContext Context => new()
    {
        Company = _firma,
        Customer = _kunde,
        PrimaryColor = _settings.PdfPrimaryColor ?? PdfColors.DefaultPrimary,
        AccentColor = _settings.PdfAccentColor ?? PdfColors.DefaultAccent
    };

    protected override PdfDocumentInfo Metadata => new()
    {
        DocumentType = _settings.PdfHeaderText ?? "Angebot",
        DocumentNumber = _angebot.Angebotsnummer,
        DocumentDate = _angebot.Erstelldatum,
        CustomerNumber = _kunde.CustomerNumber,
        DueDate = _angebot.GueltigBis,
        DueDateLabel = "Gültig bis:",
        Reference = _angebot.Referenz
    };

    protected override DocumentTexts Texts => new()
    {
        Greeting = !string.IsNullOrWhiteSpace(_kunde.Salutation)
            ? _kunde.Salutation
            : "Sehr geehrte Damen und Herren,",
        Introduction = !string.IsNullOrWhiteSpace(_angebot.Einleitung)
            ? _angebot.Einleitung
            : "gerne unterbreiten wir Ihnen folgendes Angebot:",
        ClosingText = GetClosingText()
    };

    protected override IEnumerable<DocumentLineItem> LineItems =>
        _angebot.Positionen.OrderBy(p => p.Position).Select(p => new DocumentLineItem
        {
            Position = p.Position,
            Description = p.Text,
            Quantity = p.Menge,
            UnitPrice = p.Einzelpreis,
            VatRate = p.Steuersatz
        });

    protected override DocumentSummary Summary => new()
    {
        TotalNet = _angebot.Nettosumme,
        VatGroups = _angebot.Positionen
            .GroupBy(p => p.Steuersatz)
            .Select(g => new VatGroup { Rate = g.Key, Amount = g.Sum(p => p.Steuerbetrag) })
            .OrderBy(v => v.Rate)
            .ToList()
    };

    protected override bool ShowVatColumn => !_firma.IsKleinunternehmer;
    protected override bool IsKleinunternehmer => _firma.IsKleinunternehmer;

    /// <summary>
    /// Rendert zusätzliche Inhalte nach der Zusammenfassung (z.B. Gültigkeitshinweis).
    /// </summary>
    protected override void RenderAdditionalContent(ColumnDescriptor column)
    {
        // Gültigkeitshinweis
        if (!string.IsNullOrWhiteSpace(_settings.PdfValidityNotice))
        {
            var validityText = _settings.PdfValidityNotice
                .Replace("{{Gueltigkeitsdatum}}", _angebot.GueltigBis.ToString("dd.MM.yyyy"));
            column.Item().PaddingTop(20).Text(validityText)
                .FontSize(PdfFonts.Small)
                .FontColor(PdfColors.TextSecondary);
        }
    }

    private string GetClosingText()
    {
        if (!string.IsNullOrWhiteSpace(_angebot.Schlusstext))
            return _angebot.Schlusstext;

        if (!string.IsNullOrWhiteSpace(_settings.PdfFooterText))
            return _settings.PdfFooterText;

        return "Wir freuen uns auf Ihre Rückmeldung und stehen für Rückfragen gerne zur Verfügung.";
    }

    // IDocument implementation
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(QuestPDF.Helpers.PageSizes.A4);
            page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(PdfColors.TextPrimary));

            page.Header().Element(RenderHeader);
            page.Content().Element(RenderContent);
            page.Footer().Element(RenderFooter);
        });
    }
}
