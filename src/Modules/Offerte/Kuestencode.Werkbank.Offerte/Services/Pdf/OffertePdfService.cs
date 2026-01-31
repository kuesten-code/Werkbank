using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Kuestencode.Werkbank.Offerte.Services.Pdf;

/// <summary>
/// Service zur Erzeugung von Angebots-PDFs mit QuestPDF.
/// Design angelehnt an das Faktura-Modul.
/// </summary>
public class OffertePdfService : IOffertePdfService
{
    private readonly IAngebotRepository _repository;
    private readonly ICustomerService _customerService;
    private readonly ICompanyService _companyService;
    private readonly IOfferteSettingsService _settingsService;
    private readonly ILogger<OffertePdfService> _logger;

    private readonly CultureInfo _germanCulture = new("de-DE");

    // Shared color constants
    private const string TextPrimaryColor = "#1A1A1A";
    private const string TextSecondaryColor = "#6B7280";
    private const string BackgroundColor = "#F4F6F8";
    private const string DividerColor = "#E5E7EB";

    public OffertePdfService(
        IAngebotRepository repository,
        ICustomerService customerService,
        ICompanyService companyService,
        IOfferteSettingsService settingsService,
        ILogger<OffertePdfService> logger)
    {
        _repository = repository;
        _customerService = customerService;
        _companyService = companyService;
        _settingsService = settingsService;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ErstelleAsync(Guid angebotId)
    {
        var angebot = await _repository.GetByIdAsync(angebotId);
        if (angebot == null)
        {
            throw new InvalidOperationException($"Angebot mit ID {angebotId} nicht gefunden.");
        }

        var kunde = await _customerService.GetByIdAsync(angebot.KundeId);
        if (kunde == null)
        {
            throw new InvalidOperationException($"Kunde mit ID {angebot.KundeId} nicht gefunden.");
        }

        var firma = await _companyService.GetCompanyAsync();
        var settings = await _settingsService.GetSettingsAsync();

        return Erstelle(angebot, kunde, firma, settings);
    }

    public byte[] Erstelle(Angebot angebot, Customer kunde, Company firma)
    {
        // Load settings synchronously for backwards compatibility
        var settings = _settingsService.GetSettingsAsync().GetAwaiter().GetResult();
        return Erstelle(angebot, kunde, firma, settings);
    }

    private byte[] Erstelle(Angebot angebot, Customer kunde, Company firma, OfferteSettings settings)
    {
        _logger.LogInformation("PDF-Generierung gestartet für Angebot {Angebotsnummer}", angebot.Angebotsnummer);

        var primaryColor = settings.PdfPrimaryColor ?? "#1f3a5f";
        var accentColor = settings.PdfAccentColor ?? "#3FA796";

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(2, Unit.Centimetre);
                page.MarginBottom(2, Unit.Centimetre);
                page.MarginLeft(2, Unit.Centimetre);
                page.MarginRight(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(TextPrimaryColor));

                page.Header().Element(c => RenderHeader(c, angebot, kunde, firma, settings, primaryColor));
                page.Content().Element(c => RenderContent(c, angebot, kunde, firma, settings, primaryColor, accentColor));
                page.Footer().Element(c => RenderFooter(c, firma, settings));
            });
        });

        var pdfBytes = document.GeneratePdf();
        _logger.LogInformation("PDF-Generierung abgeschlossen für Angebot {Angebotsnummer}, Größe: {Size} Bytes",
            angebot.Angebotsnummer, pdfBytes.Length);

        return pdfBytes;
    }

    private void RenderHeader(IContainer container, Angebot angebot, Customer kunde, Company firma, OfferteSettings settings, string primaryColor)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                // Left side: Company data
                row.RelativeItem().Column(leftColumn =>
                {
                    if (!string.IsNullOrEmpty(firma.BusinessName))
                    {
                        leftColumn.Item().Text(firma.BusinessName)
                            .FontSize(16)
                            .Bold()
                            .FontColor(primaryColor);
                        leftColumn.Item().Text(firma.OwnerFullName)
                            .FontSize(12)
                            .FontColor(TextSecondaryColor);
                    }
                    else
                    {
                        leftColumn.Item().Text(firma.OwnerFullName)
                            .FontSize(16)
                            .Bold()
                            .FontColor(primaryColor);
                    }

                    leftColumn.Item().PaddingTop(5).Text(firma.Address)
                        .FontSize(9)
                        .FontColor(TextSecondaryColor);
                    leftColumn.Item().Text($"{firma.PostalCode} {firma.City}")
                        .FontSize(9)
                        .FontColor(TextSecondaryColor);

                    if (!string.IsNullOrEmpty(firma.Email))
                    {
                        leftColumn.Item().PaddingTop(3).Text(firma.Email)
                            .FontSize(9)
                            .FontColor(TextSecondaryColor);
                    }
                    if (!string.IsNullOrEmpty(firma.Phone))
                    {
                        leftColumn.Item().Text(firma.Phone)
                            .FontSize(9)
                            .FontColor(TextSecondaryColor);
                    }
                });

                // Right side: Logo (if available) + Angebot metadata
                row.RelativeItem().AlignRight().Column(rightColumn =>
                {
                    // Show logo if available
                    if (firma.LogoData != null && firma.LogoData.Length > 0)
                    {
                        rightColumn.Item().MaxWidth(150).Image(firma.LogoData);
                        rightColumn.Item().PaddingBottom(10);
                    }

                    var headerText = settings.PdfHeaderText ?? "Angebot";
                    rightColumn.Item().Text($"{headerText} {angebot.Angebotsnummer}")
                        .FontSize(16)
                        .Bold()
                        .FontColor(primaryColor);

                    rightColumn.Item().PaddingTop(5).Text($"Datum: {angebot.Erstelldatum:dd.MM.yyyy}")
                        .FontSize(10);

                    rightColumn.Item().Text($"Kundennr.: {kunde.CustomerNumber}")
                        .FontSize(10);

                    rightColumn.Item().Text($"Gültig bis: {angebot.GueltigBis:dd.MM.yyyy}")
                        .FontSize(10)
                        .Bold();

                    if (!string.IsNullOrEmpty(angebot.Referenz))
                    {
                        rightColumn.Item().PaddingTop(3).Text("Referenz:")
                            .FontSize(9)
                            .FontColor(TextSecondaryColor);
                        rightColumn.Item().Text(angebot.Referenz)
                            .FontSize(9);
                    }
                });
            });

            // Divider line
            column.Item().PaddingTop(15).PaddingBottom(10)
                .BorderBottom(1)
                .BorderColor(DividerColor);
        });
    }

    private void RenderContent(IContainer container, Angebot angebot, Customer kunde, Company firma, OfferteSettings settings, string primaryColor, string accentColor)
    {
        container.Column(column =>
        {
            // Recipient address
            column.Item().PaddingTop(20).Column(addressColumn =>
            {
                addressColumn.Item().Text(kunde.Name)
                    .FontSize(11)
                    .Bold();
                addressColumn.Item().Text(kunde.Address)
                    .FontSize(10);
                addressColumn.Item().Text($"{kunde.PostalCode} {kunde.City}")
                    .FontSize(10);
            });

            // Greeting / Salutation
            column.Item().PaddingTop(30).Column(greetingColumn =>
            {
                // Use customer-specific salutation if available, otherwise default
                var salutation = !string.IsNullOrWhiteSpace(kunde.Salutation)
                    ? kunde.Salutation
                    : "Sehr geehrte Damen und Herren,";

                greetingColumn.Item().Text(salutation)
                    .FontSize(10);

                // Einleitung
                if (!string.IsNullOrEmpty(angebot.Einleitung))
                {
                    greetingColumn.Item().PaddingTop(10).Text(angebot.Einleitung)
                        .FontSize(10);
                }
                else
                {
                    greetingColumn.Item().PaddingTop(10).Text("gerne unterbreiten wir Ihnen folgendes Angebot:")
                        .FontSize(10);
                }
            });

            // Items table
            RenderItemsTable(column, angebot, firma, primaryColor);

            // Summary block
            column.Item().PaddingTop(15).Element(c =>
                RenderSummaryBlock(c, angebot, firma, accentColor));

            // Kleinunternehmer notice
            if (firma.IsKleinunternehmer)
            {
                column.Item().PaddingTop(15).Text("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.")
                    .FontSize(9)
                    .Italic()
                    .FontColor(TextSecondaryColor);
            }

            // Validity notice
            if (!string.IsNullOrWhiteSpace(settings.PdfValidityNotice))
            {
                var validityText = settings.PdfValidityNotice
                    .Replace("{{Gueltigkeitsdatum}}", angebot.GueltigBis.ToString("dd.MM.yyyy"));
                column.Item().PaddingTop(20).Text(validityText)
                    .FontSize(9)
                    .FontColor(TextSecondaryColor);
            }

            // Closing text
            column.Item().PaddingTop(20);
            if (!string.IsNullOrEmpty(angebot.Schlusstext))
            {
                column.Item().Text(angebot.Schlusstext).FontSize(10);
            }
            else if (!string.IsNullOrWhiteSpace(settings.PdfFooterText))
            {
                column.Item().Text(settings.PdfFooterText).FontSize(10);
            }
            else
            {
                column.Item().Text("Wir freuen uns auf Ihre Rückmeldung und stehen für Rückfragen gerne zur Verfügung.")
                    .FontSize(10);
            }
        });
    }

    private void RenderItemsTable(ColumnDescriptor column, Angebot angebot, Company firma, string primaryColor)
    {
        column.Item().PaddingTop(20).Table(table =>
        {
            // Define columns - without VAT column if Kleinunternehmer
            if (firma.IsKleinunternehmer)
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
            else
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

            // Header
            table.Header(header =>
            {
                header.Cell().Background(primaryColor)
                    .Padding(5).Text("Pos.").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(primaryColor)
                    .Padding(5).Text("Beschreibung").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(primaryColor)
                    .Padding(5).AlignRight().Text("Menge").FontColor("#FFFFFF").FontSize(9).Bold();
                header.Cell().Background(primaryColor)
                    .Padding(5).AlignRight().Text("Einzelpreis").FontColor("#FFFFFF").FontSize(9).Bold();

                if (!firma.IsKleinunternehmer)
                {
                    header.Cell().Background(primaryColor)
                        .Padding(5).AlignRight().Text("MwSt").FontColor("#FFFFFF").FontSize(9).Bold();
                }

                header.Cell().Background(primaryColor)
                    .Padding(5).AlignRight().Text("Gesamt").FontColor("#FFFFFF").FontSize(9).Bold();
            });

            // Items
            var orderedItems = angebot.Positionen.OrderBy(p => p.Position).ToList();
            for (int i = 0; i < orderedItems.Count; i++)
            {
                var item = orderedItems[i];
                var bgColor = i % 2 == 0 ? "#FFFFFF" : BackgroundColor;

                table.Cell().Background(bgColor).Padding(5)
                    .Text(item.Position.ToString()).FontSize(9);
                table.Cell().Background(bgColor).Padding(5)
                    .Text(text => AppendMultilineText(text, item.Text, 9));
                table.Cell().Background(bgColor).Padding(5).AlignRight()
                    .Text(item.Menge.ToString("N3", _germanCulture)).FontSize(9);
                table.Cell().Background(bgColor).Padding(5).AlignRight()
                    .Text(item.Einzelpreis.ToString("C2", _germanCulture)).FontSize(9);

                if (!firma.IsKleinunternehmer)
                {
                    table.Cell().Background(bgColor).Padding(5).AlignRight()
                        .Text($"{item.Steuersatz:N0}%").FontSize(9);
                }

                table.Cell().Background(bgColor).Padding(5).AlignRight()
                    .Text(item.Nettosumme.ToString("C2", _germanCulture)).FontSize(9);
            }
        });
    }

    private void RenderSummaryBlock(IContainer container, Angebot angebot, Company firma, string accentColor)
    {
        container.AlignRight().Width(250).Column(sumColumn =>
        {
            // Net total
            sumColumn.Item().Row(row =>
            {
                row.RelativeItem().Text("Nettosumme:").FontSize(10);
                row.ConstantItem(100).AlignRight().Text(angebot.Nettosumme.ToString("C2", _germanCulture)).FontSize(10);
            });

            // VAT
            if (firma.IsKleinunternehmer)
            {
                sumColumn.Item().PaddingTop(3).Row(row =>
                {
                    row.RelativeItem().Text("MwSt (0% §19 UStG):").FontSize(10).FontColor(TextSecondaryColor);
                    row.ConstantItem(100).AlignRight().Text(0m.ToString("C2", _germanCulture)).FontSize(10);
                });
            }
            else
            {
                // Group by VAT rate
                var steuersaetze = angebot.Positionen
                    .GroupBy(p => p.Steuersatz)
                    .Select(g => new { Satz = g.Key, Betrag = g.Sum(p => p.Steuerbetrag) })
                    .OrderBy(s => s.Satz)
                    .ToList();

                foreach (var steuer in steuersaetze)
                {
                    sumColumn.Item().PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text($"MwSt ({steuer.Satz:N0}%):").FontSize(10).FontColor(TextSecondaryColor);
                        row.ConstantItem(100).AlignRight().Text(steuer.Betrag.ToString("C2", _germanCulture)).FontSize(10);
                    });
                }
            }

            // Gross total
            sumColumn.Item().PaddingTop(8)
                .BorderTop(1)
                .BorderColor(DividerColor)
                .PaddingTop(5);

            sumColumn.Item().Row(row =>
            {
                row.RelativeItem().Text("Gesamtbetrag:").FontSize(11).Bold();
                row.ConstantItem(100).AlignRight().Text(angebot.Bruttosumme.ToString("C2", _germanCulture)).FontSize(11).Bold();
            });
        });
    }

    private void RenderFooter(IContainer container, Company firma, OfferteSettings settings)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(DividerColor).PaddingTop(10);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(leftColumn =>
                {
                    // Bei Kleinunternehmern muss der vollständige bürgerliche Name im Footer stehen
                    leftColumn.Item().Text(firma.OwnerFullName).FontSize(8).FontColor(TextSecondaryColor);
                    var taxLine = GetTaxIdentifierLine(firma);
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
                    if (!string.IsNullOrEmpty(firma.Email))
                    {
                        rightColumn.Item().Text(firma.Email).FontSize(8).FontColor(TextSecondaryColor);
                    }
                    if (!string.IsNullOrEmpty(firma.Phone))
                    {
                        rightColumn.Item().Text(firma.Phone).FontSize(8).FontColor(TextSecondaryColor);
                    }
                    if (!string.IsNullOrEmpty(firma.Website))
                    {
                        rightColumn.Item().Text(firma.Website).FontSize(8).FontColor(TextSecondaryColor);
                    }
                });
            });
        });
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
            text.Line(line).FontSize(fontSize);
        }
    }
}
