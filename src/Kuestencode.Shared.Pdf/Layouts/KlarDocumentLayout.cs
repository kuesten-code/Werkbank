using Kuestencode.Core.Models;
using Kuestencode.Shared.Pdf.Core;
using Kuestencode.Shared.Pdf.Styling;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Kuestencode.Shared.Pdf.Layouts;

/// <summary>
/// "Klar" Layout - Sauberes, minimalistisches Design mit wenigen visuellen Elementen.
/// </summary>
public abstract class KlarDocumentLayout : BaseDocumentLayout
{
    protected abstract PdfDocumentContext Context { get; }
    protected abstract PdfDocumentInfo Metadata { get; }
    protected abstract DocumentTexts Texts { get; }
    protected abstract IEnumerable<DocumentLineItem> LineItems { get; }
    protected abstract DocumentSummary Summary { get; }
    protected abstract bool ShowVatColumn { get; }
    protected abstract bool IsKleinunternehmer { get; }

    /// <summary>
    /// Rendert den Header im Klar-Stil.
    /// </summary>
    public virtual void RenderHeader(IContainer container)
    {
        var company = Context.Company;
        var primaryColor = Context.PrimaryColor;

        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                // Links: Firmendaten
                row.RelativeItem().Column(leftColumn =>
                {
                    if (!string.IsNullOrEmpty(company.BusinessName))
                    {
                        leftColumn.Item().Text(company.BusinessName)
                            .FontSize(PdfFonts.Title)
                            .Bold()
                            .FontColor(primaryColor);
                        leftColumn.Item().Text(company.OwnerFullName)
                            .FontSize(PdfFonts.Subtitle)
                            .FontColor(PdfColors.TextSecondary);
                    }
                    else
                    {
                        leftColumn.Item().Text(company.OwnerFullName)
                            .FontSize(PdfFonts.Title)
                            .Bold()
                            .FontColor(primaryColor);
                    }

                    leftColumn.Item().PaddingTop(5).Text(company.Address)
                        .FontSize(PdfFonts.Small)
                        .FontColor(PdfColors.TextSecondary);
                    leftColumn.Item().Text($"{company.PostalCode} {company.City}")
                        .FontSize(PdfFonts.Small)
                        .FontColor(PdfColors.TextSecondary);

                    if (!string.IsNullOrEmpty(company.Email))
                    {
                        leftColumn.Item().PaddingTop(3).Text(company.Email)
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                    }
                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        leftColumn.Item().Text(company.Phone)
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                    }
                });

                // Rechts: Logo (falls vorhanden) + Dokumentmetadaten
                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    // Logo
                    if (company.LogoData != null && company.LogoData.Length > 0)
                    {
                        rightColumn.Item().MaxWidth(150).Image(company.LogoData);
                        rightColumn.Item().PaddingBottom(10);
                    }

                    rightColumn.Item().Text($"{Metadata.DocumentType} {Metadata.DocumentNumber}")
                        .FontSize(PdfFonts.Title)
                        .Bold()
                        .FontColor(primaryColor);

                    rightColumn.Item().PaddingTop(5).Text($"Datum: {Metadata.DocumentDate:dd.MM.yyyy}")
                        .FontSize(PdfFonts.Body);

                    rightColumn.Item().Text($"Kundennr.: {Metadata.CustomerNumber}")
                        .FontSize(PdfFonts.Body);

                    if (Metadata.DueDate.HasValue)
                    {
                        rightColumn.Item().Text($"{Metadata.DueDateLabel} {Metadata.DueDate.Value:dd.MM.yyyy}")
                            .FontSize(PdfFonts.Body)
                            .Bold();
                    }

                    if (Metadata.ServicePeriodStart.HasValue && Metadata.ServicePeriodEnd.HasValue)
                    {
                        rightColumn.Item().PaddingTop(3).Text("Leistungszeitraum:")
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                        rightColumn.Item().Text($"{Metadata.ServicePeriodStart.Value:dd.MM.yyyy} - {Metadata.ServicePeriodEnd.Value:dd.MM.yyyy}")
                            .FontSize(PdfFonts.Small);
                    }

                    if (!string.IsNullOrEmpty(Metadata.Reference))
                    {
                        rightColumn.Item().PaddingTop(3).Text("Referenz:")
                            .FontSize(PdfFonts.Small)
                            .FontColor(PdfColors.TextSecondary);
                        rightColumn.Item().Text(Metadata.Reference)
                            .FontSize(PdfFonts.Small);
                    }
                });
            });

            // Trennlinie
            column.Item().PaddingTop(15).PaddingBottom(10)
                .BorderBottom(1)
                .BorderColor(PdfColors.Divider);
        });
    }

    /// <summary>
    /// Rendert den Content im Klar-Stil.
    /// </summary>
    public virtual void RenderContent(IContainer container)
    {
        container.Column(column =>
        {
            // Empfängeradresse
            column.Item().PaddingTop(20).Column(addressColumn =>
            {
                RenderRecipientAddress(addressColumn, Context.Customer);
            });

            // Anrede/Begrüßung
            column.Item().PaddingTop(30).Column(greetingColumn =>
            {
                RenderGreeting(greetingColumn, Texts, Context.Customer);
            });

            // Positionstabelle
            RenderItemsTable(column, LineItems, Context.PrimaryColor, ShowVatColumn, withBorder: false);

            // Summenblock
            column.Item().PaddingTop(15).Element(c =>
                RenderSummaryStandard(c, Summary, IsKleinunternehmer));

            // Kleinunternehmer-Hinweis
            RenderKleinunternehmerNotice(column, IsKleinunternehmer);

            // Hook für zusätzliche Inhalte (z.B. Zahlungsinformationen bei Rechnungen)
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
        // Wird von Faktura überschrieben für Zahlungsinformationen
    }

    /// <summary>
    /// Rendert den Footer im Klar-Stil.
    /// </summary>
    public virtual void RenderFooter(IContainer container)
    {
        RenderStandardFooter(container, Context.Company);
    }
}
