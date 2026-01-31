using Kuestencode.Core.Models;
using Kuestencode.Shared.Pdf.Core;
using Kuestencode.Shared.Pdf.Styling;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Kuestencode.Shared.Pdf.Layouts;

/// <summary>
/// "Strukturiert" Layout - Boxen und Trennlinien für klare visuelle Struktur.
/// </summary>
public abstract class StrukturiertDocumentLayout : BaseDocumentLayout
{
    protected abstract PdfDocumentContext Context { get; }
    protected abstract PdfDocumentInfo Metadata { get; }
    protected abstract DocumentTexts Texts { get; }
    protected abstract IEnumerable<DocumentLineItem> LineItems { get; }
    protected abstract DocumentSummary Summary { get; }
    protected abstract bool ShowVatColumn { get; }
    protected abstract bool IsKleinunternehmer { get; }

    /// <summary>
    /// Rendert den Header im Strukturiert-Stil mit Rahmen und Boxen.
    /// </summary>
    public virtual void RenderHeader(IContainer container)
    {
        var company = Context.Company;
        var primaryColor = Context.PrimaryColor;
        var accentColor = Context.AccentColor;

        container.Column(column =>
        {
            // Oberer farbiger Balken
            column.Item().Background(primaryColor).Height(8);

            column.Item().PaddingTop(15).Row(row =>
            {
                // Links: Firmeninformationen
                row.RelativeItem().Column(leftColumn =>
                {
                    if (company.LogoData != null && company.LogoData.Length > 0)
                    {
                        leftColumn.Item().MaxWidth(130).Image(company.LogoData);
                        leftColumn.Item().PaddingTop(5);
                    }

                    if (!string.IsNullOrEmpty(company.BusinessName))
                    {
                        leftColumn.Item().Text(company.BusinessName)
                            .FontSize(PdfFonts.Title)
                            .Bold()
                            .FontColor(primaryColor);
                        leftColumn.Item().Text(company.OwnerFullName)
                            .FontSize(PdfFonts.Body)
                            .FontColor(PdfColors.TextSecondary);
                    }
                    else
                    {
                        leftColumn.Item().Text(company.OwnerFullName)
                            .FontSize(PdfFonts.Title)
                            .Bold()
                            .FontColor(primaryColor);
                    }

                    leftColumn.Item().PaddingTop(3).Text(company.Address)
                        .FontSize(PdfFonts.Small)
                        .FontColor(PdfColors.TextSecondary);
                    leftColumn.Item().Text($"{company.PostalCode} {company.City}")
                        .FontSize(PdfFonts.Small)
                        .FontColor(PdfColors.TextSecondary);
                });

                // Rechts: Dokumentinfo-Box mit Rahmen
                row.RelativeItem().AlignRight()
                    .Border(1).BorderColor(accentColor).Padding(10).Column(rightColumn =>
                {
                    rightColumn.Item().Text(Metadata.DocumentType.ToUpper())
                        .FontSize(PdfFonts.Subtitle)
                        .Bold()
                        .FontColor(primaryColor);

                    rightColumn.Item().Text(Metadata.DocumentNumber)
                        .FontSize(PdfFonts.Title)
                        .Bold()
                        .FontColor(primaryColor);

                    rightColumn.Item().PaddingTop(5).LineHorizontal(1).LineColor(PdfColors.Divider);

                    rightColumn.Item().PaddingTop(5).Text($"Datum: {Metadata.DocumentDate:dd.MM.yyyy}")
                        .FontSize(PdfFonts.Small);
                    rightColumn.Item().Text($"Kundennr.: {Metadata.CustomerNumber}")
                        .FontSize(PdfFonts.Small);

                    if (Metadata.DueDate.HasValue)
                    {
                        rightColumn.Item().Text($"{Metadata.DueDateLabel} {Metadata.DueDate.Value:dd.MM.yyyy}")
                            .FontSize(PdfFonts.Small)
                            .Bold();
                    }

                    if (Metadata.ServicePeriodStart.HasValue && Metadata.ServicePeriodEnd.HasValue)
                    {
                        rightColumn.Item().PaddingTop(3).Text("Leistungszeitraum:")
                            .FontSize(PdfFonts.Footer)
                            .FontColor(PdfColors.TextSecondary);
                        rightColumn.Item().Text($"{Metadata.ServicePeriodStart.Value:dd.MM.yyyy} - {Metadata.ServicePeriodEnd.Value:dd.MM.yyyy}")
                            .FontSize(PdfFonts.Footer);
                    }

                    if (!string.IsNullOrEmpty(Metadata.Reference))
                    {
                        rightColumn.Item().PaddingTop(3).Text($"Ref: {Metadata.Reference}")
                            .FontSize(PdfFonts.Footer)
                            .FontColor(PdfColors.TextSecondary);
                    }
                });
            });

            column.Item().PaddingTop(10);
        });
    }

    /// <summary>
    /// Rendert den Content im Strukturiert-Stil.
    /// </summary>
    public virtual void RenderContent(IContainer container)
    {
        var accentColor = Context.AccentColor;

        container.Column(column =>
        {
            // Empfängeradresse in Box
            column.Item().PaddingTop(10)
                .Border(1).BorderColor(accentColor).Padding(10).Column(addressColumn =>
            {
                addressColumn.Item().Text("Empfänger")
                    .FontSize(PdfFonts.Footer)
                    .FontColor(PdfColors.TextSecondary);
                addressColumn.Item().PaddingTop(3);
                RenderRecipientAddress(addressColumn, Context.Customer);
            });

            // Anrede/Begrüßung
            column.Item().PaddingTop(20).Column(greetingColumn =>
            {
                RenderGreeting(greetingColumn, Texts, Context.Customer);
            });

            // Positionstabelle mit Rahmen
            RenderItemsTable(column, LineItems, Context.PrimaryColor, ShowVatColumn, withBorder: true);

            // Summenblock mit Rahmen
            column.Item().PaddingTop(15).Element(c =>
                RenderSummaryWithBorder(c, Summary, IsKleinunternehmer, accentColor));

            // Kleinunternehmer-Hinweis
            RenderKleinunternehmerNotice(column, IsKleinunternehmer);

            // Hook für zusätzliche Inhalte
            RenderAdditionalContent(column);

            // Schlusstext
            RenderClosingText(column, Texts.ClosingText, bold: false);
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
    /// Rendert den Footer im Strukturiert-Stil.
    /// </summary>
    public virtual void RenderFooter(IContainer container)
    {
        RenderStandardFooter(container, Context.Company);
    }
}
