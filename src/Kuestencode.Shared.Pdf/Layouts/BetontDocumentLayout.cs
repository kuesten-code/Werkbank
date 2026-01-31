using Kuestencode.Core.Models;
using Kuestencode.Shared.Pdf.Core;
using Kuestencode.Shared.Pdf.Styling;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Kuestencode.Shared.Pdf.Layouts;

/// <summary>
/// "Betont" Layout - Kräftiges Design mit farbigem Header und Hervorhebungen.
/// </summary>
public abstract class BetontDocumentLayout : BaseDocumentLayout
{
    protected abstract PdfDocumentContext Context { get; }
    protected abstract PdfDocumentInfo Metadata { get; }
    protected abstract DocumentTexts Texts { get; }
    protected abstract IEnumerable<DocumentLineItem> LineItems { get; }
    protected abstract DocumentSummary Summary { get; }
    protected abstract bool ShowVatColumn { get; }
    protected abstract bool IsKleinunternehmer { get; }

    /// <summary>
    /// Rendert den Header im Betont-Stil mit großem farbigen Balken.
    /// </summary>
    public virtual void RenderHeader(IContainer container)
    {
        var company = Context.Company;
        var primaryColor = Context.PrimaryColor;
        var accentColor = Context.AccentColor;

        container.Column(column =>
        {
            // Farbiger Header-Balken
            column.Item().Background(primaryColor).Padding(15).Row(row =>
            {
                // Links: Firmeninformationen
                row.RelativeItem().Column(leftColumn =>
                {
                    if (company.LogoData != null && company.LogoData.Length > 0)
                    {
                        leftColumn.Item().MaxWidth(120).Image(company.LogoData);
                    }
                    else
                    {
                        leftColumn.Item().Text(company.BusinessName ?? company.OwnerFullName)
                            .FontSize(18)
                            .Bold()
                            .FontColor(PdfColors.White);
                    }

                    leftColumn.Item().PaddingTop(5).Text($"{company.Address}, {company.PostalCode} {company.City}")
                        .FontSize(PdfFonts.Small)
                        .FontColor(PdfColors.White);
                });

                // Rechts: Dokumenttyp
                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    rightColumn.Item().Text(Metadata.DocumentType.ToUpper())
                        .FontSize(24)
                        .Bold()
                        .FontColor(PdfColors.White);
                });
            });

            // Dokumentmetadaten mit Akzentfarbe
            column.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Column(leftColumn =>
                {
                    // Leere linke Spalte oder Kontaktdaten
                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        leftColumn.Item().Text($"Tel: {company.Phone}")
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                    }
                    if (!string.IsNullOrEmpty(company.Email))
                    {
                        leftColumn.Item().Text(company.Email)
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                    }
                });

                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    // Dokumentnummer hervorgehoben
                    rightColumn.Item().Background(accentColor).Padding(5)
                        .Text(Metadata.DocumentNumber)
                        .FontSize(PdfFonts.Subtitle)
                        .Bold()
                        .FontColor(PdfColors.White);

                    rightColumn.Item().PaddingTop(5).Text($"Datum: {Metadata.DocumentDate:dd.MM.yyyy}")
                        .FontSize(PdfFonts.Body);
                    rightColumn.Item().Text($"Kundennr.: {Metadata.CustomerNumber}")
                        .FontSize(PdfFonts.Body);

                    if (Metadata.DueDate.HasValue)
                    {
                        rightColumn.Item().Background(accentColor).Padding(3).PaddingHorizontal(5)
                            .Text($"{Metadata.DueDateLabel} {Metadata.DueDate.Value:dd.MM.yyyy}")
                            .FontSize(PdfFonts.Body)
                            .Bold()
                            .FontColor(PdfColors.White);
                    }

                    if (!string.IsNullOrEmpty(Metadata.Reference))
                    {
                        rightColumn.Item().PaddingTop(3).Text($"Ref: {Metadata.Reference}")
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                    }
                });
            });

            column.Item().PaddingTop(10);
        });
    }

    /// <summary>
    /// Rendert den Content im Betont-Stil.
    /// </summary>
    public virtual void RenderContent(IContainer container)
    {
        var accentColor = Context.AccentColor;

        container.Column(column =>
        {
            // Empfängeradresse mit Akzentrahmen
            column.Item().PaddingTop(10).BorderLeft(3).BorderColor(accentColor).PaddingLeft(10).Column(addressColumn =>
            {
                RenderRecipientAddress(addressColumn, Context.Customer);
            });

            // Anrede/Begrüßung
            column.Item().PaddingTop(25).Column(greetingColumn =>
            {
                RenderGreeting(greetingColumn, Texts, Context.Customer);
            });

            // Positionstabelle
            RenderItemsTable(column, LineItems, Context.PrimaryColor, ShowVatColumn, withBorder: false);

            // Summenblock mit Hintergrund
            column.Item().PaddingTop(15).Element(c =>
                RenderSummaryWithBackground(c, Summary, IsKleinunternehmer, accentColor));

            // Kleinunternehmer-Hinweis
            RenderKleinunternehmerNotice(column, IsKleinunternehmer);

            // Hook für zusätzliche Inhalte
            RenderAdditionalContent(column);

            // Schlusstext (fett)
            RenderClosingText(column, Texts.ClosingText, bold: true);
        });
    }

    /// <summary>
    /// Hook für modulspezifische zusätzliche Inhalte.
    /// </summary>
    protected virtual void RenderAdditionalContent(ColumnDescriptor column)
    {
        // Standard: keine zusätzlichen Inhalte
    }

    /// <summary>
    /// Rendert den Footer im Betont-Stil.
    /// </summary>
    public virtual void RenderFooter(IContainer container)
    {
        RenderStandardFooter(container, Context.Company);
    }
}
