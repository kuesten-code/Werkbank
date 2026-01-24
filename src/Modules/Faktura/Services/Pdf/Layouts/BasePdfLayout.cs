using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services.Pdf.Components;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Kuestencode.Faktura.Services.Pdf.Layouts;

/// <summary>
/// Base class for PDF layout renderers containing shared functionality.
/// </summary>
public abstract class BasePdfLayout : IPdfLayoutRenderer
{
    protected readonly PdfTemplateEngine TemplateEngine;
    protected readonly PdfSummaryBlockBuilder SummaryBlockBuilder;
    protected readonly PdfPaymentInfoBuilder PaymentInfoBuilder;
    protected readonly CultureInfo GermanCulture = new CultureInfo("de-DE");

    // Shared color constants
    protected const string TextPrimaryColor = "#1A1A1A";
    protected const string TextSecondaryColor = "#6B7280";
    protected const string BackgroundColor = "#F4F6F8";
    protected const string DividerColor = "#E5E7EB";

    protected BasePdfLayout(
        PdfTemplateEngine templateEngine,
        PdfSummaryBlockBuilder summaryBlockBuilder,
        PdfPaymentInfoBuilder paymentInfoBuilder)
    {
        TemplateEngine = templateEngine;
        SummaryBlockBuilder = summaryBlockBuilder;
        PaymentInfoBuilder = paymentInfoBuilder;
    }

    public abstract void RenderHeader(IContainer container, Invoice invoice, Company company);
    public abstract void RenderContent(IContainer container, Invoice invoice, Company company);

    public virtual void RenderFooter(IContainer container, Company company)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(DividerColor).PaddingTop(10);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(leftColumn =>
                {
                    // Bei Kleinunternehmern muss der vollständige bürgerliche Name im Footer stehen
                    leftColumn.Item().Text(company.OwnerFullName).FontSize(8).FontColor(TextSecondaryColor);
                    var taxLine = GetTaxIdentifierLine(company);
                    if (!string.IsNullOrWhiteSpace(taxLine))
                    {
                        leftColumn.Item().Text(taxLine).FontSize(8).FontColor(TextSecondaryColor);
                    }
                });

                row.RelativeItem().AlignCenter().Column(centerColumn =>
                {
                    centerColumn.Item().Text(text =>
                    {
                        text.CurrentPageNumber().FontSize(8).FontColor(TextSecondaryColor);
                        text.Span(" / ").FontSize(8).FontColor(TextSecondaryColor);
                        text.TotalPages().FontSize(8).FontColor(TextSecondaryColor);
                    });
                });

                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    rightColumn.Item().Text($"{company.BankName}").FontSize(8).FontColor(TextSecondaryColor);
                    rightColumn.Item().Text($"IBAN: {company.BankAccount}").FontSize(8).FontColor(TextSecondaryColor);
                });
            });
        });
    }

    /// <summary>
    /// Renders the recipient address section.
    /// </summary>
    protected void RenderRecipientAddress(ColumnDescriptor column, Invoice invoice)
    {
        if (invoice.Customer != null)
        {
            column.Item().Text(invoice.Customer.Name)
                .FontSize(11)
                .Bold();
            column.Item().Text(invoice.Customer.Address)
                .FontSize(10);
            column.Item().Text($"{invoice.Customer.PostalCode} {invoice.Customer.City}")
                .FontSize(10);
        }
    }

    /// <summary>
    /// Renders the greeting and introduction text.
    /// </summary>
    protected void RenderGreeting(ColumnDescriptor column, Invoice invoice, Company company)
    {
        if (!string.IsNullOrWhiteSpace(company.PdfHeaderText))
        {
            column.Item().Text(TemplateEngine.ReplacePlaceholders(company.PdfHeaderText, invoice, company))
                .FontSize(10);
        }
        else
        {
            column.Item().Text("Sehr geehrte Damen und Herren,")
                .FontSize(10);
            column.Item().PaddingTop(10).Text("hiermit stellen wir Ihnen folgende Leistungen in Rechnung:")
                .FontSize(10);
        }
    }

    /// <summary>
    /// Renders the items table with positions, descriptions, quantities, prices.
    /// </summary>
    protected void RenderItemsTable(ColumnDescriptor column, Invoice invoice, Company company, bool withBorder = false)
    {
        var tableContainer = column.Item().PaddingTop(20);

        if (withBorder)
        {
            tableContainer = tableContainer.Border(1).BorderColor(company.PdfPrimaryColor);
        }

        tableContainer.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(40);  // Pos.
                columns.RelativeColumn(4);   // Beschreibung
                columns.ConstantColumn(60);  // Menge
                columns.ConstantColumn(80);  // Einzelpreis
                columns.ConstantColumn(80);  // Gesamtpreis
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Background(company.PdfPrimaryColor)
                    .Padding(5).Text("Pos.").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(company.PdfPrimaryColor)
                    .Padding(5).Text("Beschreibung").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(company.PdfPrimaryColor)
                    .Padding(5).AlignRight().Text("Menge").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(company.PdfPrimaryColor)
                    .Padding(5).AlignRight().Text("Einzelpreis").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(company.PdfPrimaryColor)
                    .Padding(5).AlignRight().Text("Gesamtpreis").FontColor("#FFFFFF").FontSize(9).Bold();
            });

            // Items
            var orderedItems = invoice.Items.OrderBy(i => i.Position).ToList();
            for (int i = 0; i < orderedItems.Count; i++)
            {
                var item = orderedItems[i];
                var bgColor = i % 2 == 0 ? "#FFFFFF" : BackgroundColor;

                if (withBorder)
                {
                    RenderItemRowWithBorder(table, item, bgColor);
                }
                else
                {
                    RenderItemRow(table, item, bgColor);
                }
            }
        });
    }

    private void RenderItemRow(TableDescriptor table, InvoiceItem item, string bgColor)
    {
        table.Cell().Background(bgColor).Padding(5)
            .Text(item.Position.ToString()).FontSize(9);
        table.Cell().Background(bgColor).Padding(5)
            .Text(text => AppendMultilineText(text, item.Description, 9));
        table.Cell().Background(bgColor).Padding(5).AlignRight()
            .Text(item.Quantity.ToString("N3", GermanCulture)).FontSize(9);
        table.Cell().Background(bgColor).Padding(5).AlignRight()
            .Text(item.UnitPrice.ToString("C2", GermanCulture)).FontSize(9);
        table.Cell().Background(bgColor).Padding(5).AlignRight()
            .Text(item.TotalNet.ToString("C2", GermanCulture)).FontSize(9);
    }

    private void RenderItemRowWithBorder(TableDescriptor table, InvoiceItem item, string bgColor)
    {
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(DividerColor).Padding(5)
            .Text(item.Position.ToString()).FontSize(9);
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(DividerColor).Padding(5)
            .Text(text => AppendMultilineText(text, item.Description, 9));
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(DividerColor).Padding(5).AlignRight()
            .Text(item.Quantity.ToString("N3", GermanCulture)).FontSize(9);
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(DividerColor).Padding(5).AlignRight()
            .Text(item.UnitPrice.ToString("C2", GermanCulture)).FontSize(9);
        table.Cell().Background(bgColor).BorderBottom(1).BorderColor(DividerColor).Padding(5).AlignRight()
            .Text(item.TotalNet.ToString("C2", GermanCulture)).FontSize(9);
    }

    /// <summary>
    /// Renders the "Kleinunternehmer" notice if applicable.
    /// </summary>
    protected void RenderKleinunternehmerNotice(ColumnDescriptor column, Company company)
    {
        if (company.IsKleinunternehmer)
        {
            column.Item().PaddingTop(15).Text("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.")
                .FontSize(9)
                .Italic()
                .FontColor(TextSecondaryColor);
        }
    }

    /// <summary>
    /// Renders the closing text.
    /// </summary>
    protected void RenderClosingText(ColumnDescriptor column, Invoice invoice, Company company, bool bold = false)
    {
        var text = !string.IsNullOrWhiteSpace(company.PdfFooterText)
            ? TemplateEngine.ReplacePlaceholders(company.PdfFooterText, invoice, company)
            : "Vielen Dank für Ihr Vertrauen!";

        var textStyle = column.Item().PaddingTop(20).Text(text).FontSize(10);
        if (bold) textStyle.Bold();
    }

    private static string? GetTaxIdentifierLine(Company company)
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

    private static void AppendMultilineText(TextDescriptor text, string value, float fontSize)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        var lines = value.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Length == 0 ? " " : lines[i];
            if (i == 0)
            {
                text.Span(line).FontSize(fontSize);
            }
            else
            {
                text.Line(line).FontSize(fontSize);
            }
        }
    }
}
