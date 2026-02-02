using Kuestencode.Core.Models;
using Kuestencode.Shared.Pdf.Core;
using Kuestencode.Shared.Pdf.Styling;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Kuestencode.Shared.Pdf.Layouts;

/// <summary>
/// Basis-Klasse für alle PDF-Layout-Renderer mit gemeinsamer Funktionalität.
/// </summary>
public abstract class BaseDocumentLayout
{
    protected readonly CultureInfo GermanCulture = new("de-DE");

    /// <summary>
    /// Rendert die Empfängeradresse.
    /// </summary>
    protected void RenderRecipientAddress(ColumnDescriptor column, Customer customer)
    {
        column.Item().Text(customer.Name)
            .FontSize(PdfFonts.SectionHeader)
            .Bold();
        column.Item().Text(customer.Address)
            .FontSize(PdfFonts.Body);
        column.Item().Text($"{customer.PostalCode} {customer.City}")
            .FontSize(PdfFonts.Body);
    }

    /// <summary>
    /// Rendert die Anrede/Begrüßung.
    /// </summary>
    protected void RenderGreeting(ColumnDescriptor column, DocumentTexts texts, Customer customer)
    {
        // Kundenspezifische Anrede oder Standard
        var salutation = !string.IsNullOrWhiteSpace(customer.Salutation)
            ? customer.Salutation
            : "Sehr geehrte Damen und Herren,";

        column.Item().Text(salutation)
            .FontSize(PdfFonts.Body);

        // Einleitungstext
        if (!string.IsNullOrWhiteSpace(texts.Introduction))
        {
            column.Item().PaddingTop(10).Text(texts.Introduction)
                .FontSize(PdfFonts.Body);
        }
    }

    /// <summary>
    /// Rendert die Positionstabelle.
    /// </summary>
    protected void RenderItemsTable(
        ColumnDescriptor column,
        IEnumerable<DocumentLineItem> items,
        string primaryColor,
        bool showVatColumn,
        bool withBorder = false)
    {
        var tableContainer = column.Item().PaddingTop(20);

        if (withBorder)
        {
            tableContainer = tableContainer.Border(1).BorderColor(primaryColor);
        }

        tableContainer.Table(table =>
        {
            // Spaltendefinition
            if (showVatColumn)
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  // Pos.
                    columns.RelativeColumn(4);   // Beschreibung
                    columns.ConstantColumn(60);  // Menge
                    columns.ConstantColumn(80);  // Einzelpreis
                    columns.ConstantColumn(50);  // MwSt
                    columns.ConstantColumn(80);  // Gesamtpreis
                });
            }
            else
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(40);  // Pos.
                    columns.RelativeColumn(4);   // Beschreibung
                    columns.ConstantColumn(60);  // Menge
                    columns.ConstantColumn(80);  // Einzelpreis
                    columns.ConstantColumn(80);  // Gesamtpreis
                });
            }

            // Header
            table.Header(header =>
            {
                header.Cell().Background(primaryColor)
                    .Padding(5).Text("Pos.").FontColor(PdfColors.White).FontSize(PdfFonts.Small).Bold();
                header.Cell().Background(primaryColor)
                    .Padding(5).Text("Beschreibung").FontColor(PdfColors.White).FontSize(PdfFonts.Small).Bold();
                header.Cell().Background(primaryColor)
                    .Padding(5).AlignRight().Text("Menge").FontColor(PdfColors.White).FontSize(PdfFonts.Small).Bold();
                header.Cell().Background(primaryColor)
                    .Padding(5).AlignRight().Text("Einzelpreis").FontColor(PdfColors.White).FontSize(PdfFonts.Small).Bold();

                if (showVatColumn)
                {
                    header.Cell().Background(primaryColor)
                        .Padding(5).AlignRight().Text("MwSt").FontColor(PdfColors.White).FontSize(PdfFonts.Small).Bold();
                }

                header.Cell().Background(primaryColor)
                    .Padding(5).AlignRight().Text("Gesamt").FontColor(PdfColors.White).FontSize(PdfFonts.Small).Bold();
            });

            // Positionen
            var orderedItems = items.OrderBy(i => i.Position).ToList();
            for (int i = 0; i < orderedItems.Count; i++)
            {
                var item = orderedItems[i];
                var bgColor = i % 2 == 0 ? PdfColors.White : PdfColors.BackgroundAlternate;

                if (withBorder)
                {
                    RenderItemRowWithBorder(table, item, bgColor, showVatColumn);
                }
                else
                {
                    RenderItemRow(table, item, bgColor, showVatColumn);
                }
            }
        });
    }

    private void RenderItemRow(TableDescriptor table, DocumentLineItem item, string bgColor, bool showVatColumn)
    {
        table.Cell().Background(bgColor).Padding(5)
            .Text(item.Position.ToString()).FontSize(PdfFonts.Small);
        table.Cell().Background(bgColor).Padding(5)
            .Text(text => AppendMultilineText(text, item.Description, PdfFonts.Small));
        table.Cell().Background(bgColor).Padding(5).AlignRight()
            .Text(item.Quantity.ToString("N3", GermanCulture)).FontSize(PdfFonts.Small);
        table.Cell().Background(bgColor).Padding(5).AlignRight()
            .Text(item.UnitPrice.ToString("C2", GermanCulture)).FontSize(PdfFonts.Small);

        if (showVatColumn)
        {
            table.Cell().Background(bgColor).Padding(5).AlignRight()
                .Text($"{item.VatRate:N0}%").FontSize(PdfFonts.Small);
        }

        table.Cell().Background(bgColor).Padding(5).AlignRight()
            .Text(item.TotalNet.ToString("C2", GermanCulture)).FontSize(PdfFonts.Small);
    }

    private void RenderItemRowWithBorder(TableDescriptor table, DocumentLineItem item, string bgColor, bool showVatColumn)
    {
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(PdfColors.Divider).Padding(5)
            .Text(item.Position.ToString()).FontSize(PdfFonts.Small);
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(PdfColors.Divider).Padding(5)
            .Text(text => AppendMultilineText(text, item.Description, PdfFonts.Small));
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(PdfColors.Divider).Padding(5).AlignRight()
            .Text(item.Quantity.ToString("N3", GermanCulture)).FontSize(PdfFonts.Small);
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(PdfColors.Divider).Padding(5).AlignRight()
            .Text(item.UnitPrice.ToString("C2", GermanCulture)).FontSize(PdfFonts.Small);

        if (showVatColumn)
        {
            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(PdfColors.Divider).Padding(5).AlignRight()
                .Text($"{item.VatRate:N0}%").FontSize(PdfFonts.Small);
        }

        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(PdfColors.Divider).Padding(5).AlignRight()
            .Text(item.TotalNet.ToString("C2", GermanCulture)).FontSize(PdfFonts.Small);
    }

    /// <summary>
    /// Rendert den Summenblock (Standard-Stil ohne Hintergrund).
    /// </summary>
    protected void RenderSummaryStandard(IContainer container, DocumentSummary summary, bool isKleinunternehmer)
    {
        container.AlignRight().Width(250).Column(sumColumn =>
        {
            RenderSummaryContent(sumColumn, summary, isKleinunternehmer, null);
        });
    }

    /// <summary>
    /// Rendert den Summenblock mit Rahmen.
    /// </summary>
    protected void RenderSummaryWithBorder(IContainer container, DocumentSummary summary, bool isKleinunternehmer, string accentColor)
    {
        container.AlignRight().Width(250).Border(1).BorderColor(accentColor).Padding(10).Column(sumColumn =>
        {
            RenderSummaryContent(sumColumn, summary, isKleinunternehmer, null);
        });
    }

    /// <summary>
    /// Rendert den Summenblock mit farbigem Hintergrund.
    /// </summary>
    protected void RenderSummaryWithBackground(IContainer container, DocumentSummary summary, bool isKleinunternehmer, string accentColor)
    {
        container.AlignRight().Width(250).Background(accentColor).Padding(10).Column(sumColumn =>
        {
            RenderSummaryContent(sumColumn, summary, isKleinunternehmer, PdfColors.White);
        });
    }

    private void RenderSummaryContent(ColumnDescriptor sumColumn, DocumentSummary summary, bool isKleinunternehmer, string? textColor)
    {
        // Nettosumme
        sumColumn.Item().Row(row =>
        {
            var text = row.RelativeItem().Text("Nettosumme:").FontSize(PdfFonts.Body);
            if (textColor != null) text.FontColor(textColor);

            var amountText = row.ConstantItem(100).AlignRight().Text(summary.TotalNet.ToString("C2", GermanCulture)).FontSize(PdfFonts.Body);
            if (textColor != null) amountText.FontColor(textColor);
        });

        // Rabatt (falls vorhanden)
        if (summary.DiscountAmount > 0)
        {
            sumColumn.Item().PaddingTop(3).Row(row =>
            {
                var discountText = summary.DiscountPercent.HasValue
                    ? $"Rabatt ({summary.DiscountPercent}%):"
                    : "Rabatt:";

                var labelText = row.RelativeItem().Text(discountText).FontSize(PdfFonts.Body);
                if (textColor != null)
                    labelText.FontColor(textColor);
                else
                    labelText.FontColor(PdfColors.TextSecondary);

                var amountText = row.ConstantItem(100).AlignRight().Text($"-{summary.DiscountAmount?.ToString("C2", GermanCulture)}").FontSize(PdfFonts.Body);
                if (textColor != null)
                    amountText.FontColor(textColor);
                else
                    amountText.FontColor(PdfColors.TextSecondary);
            });

            // Zwischensumme
            sumColumn.Item().PaddingTop(3).Row(row =>
            {
                var text = row.RelativeItem().Text("Zwischensumme:").FontSize(PdfFonts.Body);
                if (textColor != null) text.FontColor(textColor);

                var amountText = row.ConstantItem(100).AlignRight().Text(summary.TotalNetAfterDiscount.ToString("C2", GermanCulture)).FontSize(PdfFonts.Body);
                if (textColor != null) amountText.FontColor(textColor);
            });
        }

        // MwSt
        if (isKleinunternehmer)
        {
            sumColumn.Item().PaddingTop(3).Row(row =>
            {
                var labelText = row.RelativeItem().Text("MwSt (0% §19 UStG):").FontSize(PdfFonts.Body);
                if (textColor != null)
                    labelText.FontColor(textColor);
                else
                    labelText.FontColor(PdfColors.TextSecondary);

                var amountText = row.ConstantItem(100).AlignRight().Text(0m.ToString("C2", GermanCulture)).FontSize(PdfFonts.Body);
                if (textColor != null) amountText.FontColor(textColor);
            });
        }
        else
        {
            foreach (var vat in summary.VatGroups.OrderBy(v => v.Rate))
            {
                sumColumn.Item().PaddingTop(3).Row(row =>
                {
                    var labelText = row.RelativeItem().Text($"MwSt ({vat.Rate:N0}%):").FontSize(PdfFonts.Body);
                    if (textColor != null)
                        labelText.FontColor(textColor);
                    else
                        labelText.FontColor(PdfColors.TextSecondary);

                    var amountText = row.ConstantItem(100).AlignRight().Text(vat.Amount.ToString("C2", GermanCulture)).FontSize(PdfFonts.Body);
                    if (textColor != null) amountText.FontColor(textColor);
                });
            }
        }

        // Bruttosumme
        var borderColor = textColor == PdfColors.White ? PdfColors.White : PdfColors.Divider;
        sumColumn.Item().PaddingTop(textColor == PdfColors.White ? 5 : 8)
            .BorderTop(textColor == PdfColors.White ? 2 : 1)
            .BorderColor(borderColor)
            .PaddingTop(5);

        sumColumn.Item().Row(row =>
        {
            var fontSize = textColor == PdfColors.White ? PdfFonts.Subtitle : PdfFonts.SectionHeader;
            row.RelativeItem().Text("Bruttosumme:").FontSize(fontSize).FontColor(textColor ?? PdfColors.TextPrimary);
            row.ConstantItem(100).AlignRight().Text(summary.TotalGross.ToString("C2", GermanCulture)).FontSize(fontSize).FontColor(textColor ?? PdfColors.TextPrimary);
        });

        // Anzahlungen (falls vorhanden)
        if (summary.DownPayments?.Any() == true)
        {
            var labelText = sumColumn.Item().PaddingTop(8).Text("Abgezogen:").FontSize(PdfFonts.Small);
            if (textColor != null)
                labelText.FontColor(textColor);
            else
                labelText.FontColor(PdfColors.TextSecondary);

            foreach (var downPayment in summary.DownPayments)
            {
                sumColumn.Item().PaddingTop(2).Row(row =>
                {
                    var dateText = downPayment.PaymentDate.HasValue
                        ? $"{downPayment.Description} ({downPayment.PaymentDate.Value:dd.MM.yyyy})"
                        : downPayment.Description;

                    var descText = row.RelativeItem().Text(dateText).FontSize(PdfFonts.Small);
                    if (textColor != null)
                        descText.FontColor(textColor);
                    else
                        descText.FontColor(PdfColors.TextSecondary);

                    var amountText = row.ConstantItem(100).AlignRight().Text($"-{downPayment.Amount.ToString("C2", GermanCulture)}").FontSize(PdfFonts.Small);
                    if (textColor != null)
                        amountText.FontColor(textColor);
                    else
                        amountText.FontColor(PdfColors.TextSecondary);
                });
            }

            // Zu zahlen
            sumColumn.Item().PaddingTop(8).BorderTop(2).BorderColor(borderColor).PaddingTop(5);

            sumColumn.Item().Row(row =>
            {
                var fontSize = textColor == PdfColors.White ? 13f : 12f;
                var labelStyle = row.RelativeItem().Text("Zu zahlen:").FontSize(fontSize).Bold();
                if (textColor != null) labelStyle.FontColor(textColor);

                var amountStyle = row.ConstantItem(100).AlignRight().Text(summary.AmountDue.ToString("C2", GermanCulture)).FontSize(fontSize).Bold();
                if (textColor != null) amountStyle.FontColor(textColor);
            });
        }
    }

    /// <summary>
    /// Rendert den Kleinunternehmer-Hinweis.
    /// </summary>
    protected void RenderKleinunternehmerNotice(ColumnDescriptor column, bool isKleinunternehmer)
    {
        if (isKleinunternehmer)
        {
            column.Item().PaddingTop(15).Text("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.")
                .FontSize(PdfFonts.Small)
                .Italic()
                .FontColor(PdfColors.TextSecondary);
        }
    }

    /// <summary>
    /// Rendert den Schlusstext.
    /// </summary>
    protected void RenderClosingText(ColumnDescriptor column, string? text, bool bold = false)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "Vielen Dank für Ihr Vertrauen!";
        }

        var textStyle = column.Item().PaddingTop(20).Text(text).FontSize(PdfFonts.Body);
        if (bold) textStyle.Bold();
    }

    /// <summary>
    /// Rendert den Standard-Footer.
    /// </summary>
    protected void RenderStandardFooter(IContainer container, Company company)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(PdfColors.Divider).PaddingTop(10);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(leftColumn =>
                {
                    leftColumn.Item().Text(company.OwnerFullName).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    var taxLine = GetTaxIdentifierLine(company);
                    if (!string.IsNullOrWhiteSpace(taxLine))
                    {
                        leftColumn.Item().Text(taxLine).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                });

                row.RelativeItem().AlignCenter().Column(centerColumn =>
                {
                    centerColumn.Item().Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                        text.Span(" / ").FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                        text.TotalPages().FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    });
                });

                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    if (!string.IsNullOrEmpty(company.BankName))
                    {
                        rightColumn.Item().Text(company.BankName).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                    if (!string.IsNullOrEmpty(company.BankAccount))
                    {
                        rightColumn.Item().Text($"IBAN: {company.BankAccount}").FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                });
            });
        });
    }

    /// <summary>
    /// Rendert einen Footer ohne Bankdaten (für Angebote).
    /// </summary>
    protected void RenderFooterWithoutBank(IContainer container, Company company)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(PdfColors.Divider).PaddingTop(10);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(leftColumn =>
                {
                    leftColumn.Item().Text(company.OwnerFullName).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    var taxLine = GetTaxIdentifierLine(company);
                    if (!string.IsNullOrWhiteSpace(taxLine))
                    {
                        leftColumn.Item().Text(taxLine).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                });

                row.RelativeItem().AlignCenter().Column(centerColumn =>
                {
                    centerColumn.Item().Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                        text.Span(" / ").FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                        text.TotalPages().FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    });
                });

                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    if (!string.IsNullOrEmpty(company.Email))
                    {
                        rightColumn.Item().Text(company.Email).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        rightColumn.Item().Text(company.Phone).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                    if (!string.IsNullOrEmpty(company.Website))
                    {
                        rightColumn.Item().Text(company.Website).FontSize(PdfFonts.Footer).FontColor(PdfColors.TextSecondary);
                    }
                });
            });
        });
    }

    /// <summary>
    /// Holt die Steuer-Identifikationszeile (USt-IdNr oder Steuernr).
    /// </summary>
    protected static string? GetTaxIdentifierLine(Company company)
    {
        if (!string.IsNullOrWhiteSpace(company.VatId))
        {
            return $"USt-IdNr.: {company.VatId}";
        }

        if (!string.IsNullOrWhiteSpace(company.TaxNumber))
        {
            return $"Steuernr.: {company.TaxNumber}";
        }

        return null;
    }

    /// <summary>
    /// Hilfsmethode für mehrzeilige Texte.
    /// </summary>
    protected static void AppendMultilineText(TextDescriptor text, string value, float fontSize)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var lines = value.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Length == 0 ? " " : lines[i];
            text.Line(line).FontSize(fontSize);
        }
    }
}
