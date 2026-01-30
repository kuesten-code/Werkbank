using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kuestencode.Werkbank.Offerte.Services.Pdf;

/// <summary>
/// Service zur Erzeugung von Angebots-PDFs mit QuestPDF.
/// </summary>
public class OffertePdfService : IOffertePdfService
{
    private readonly IAngebotRepository _repository;
    private readonly ICustomerService _customerService;
    private readonly ICompanyService _companyService;
    private readonly ILogger<OffertePdfService> _logger;

    public OffertePdfService(
        IAngebotRepository repository,
        ICustomerService customerService,
        ICompanyService companyService,
        ILogger<OffertePdfService> logger)
    {
        _repository = repository;
        _customerService = customerService;
        _companyService = companyService;
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

        return Erstelle(angebot, kunde, firma);
    }

    public byte[] Erstelle(Angebot angebot, Customer kunde, Company firma)
    {
        _logger.LogInformation("PDF-Generierung gestartet für Angebot {Angebotsnummer}", angebot.Angebotsnummer);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor("#1A1A1A"));

                page.Header().Element(c => RenderHeader(c, angebot, kunde, firma));
                page.Content().Element(c => RenderContent(c, angebot, firma));
                page.Footer().Element(c => RenderFooter(c, angebot, firma));
            });
        });

        var pdfBytes = document.GeneratePdf();
        _logger.LogInformation("PDF-Generierung abgeschlossen für Angebot {Angebotsnummer}, Größe: {Size} Bytes",
            angebot.Angebotsnummer, pdfBytes.Length);

        return pdfBytes;
    }

    private void RenderHeader(IContainer container, Angebot angebot, Customer kunde, Company firma)
    {
        container.Column(column =>
        {
            // Absenderzeile
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    // Logo falls vorhanden
                    if (firma.LogoData != null && firma.LogoData.Length > 0)
                    {
                        col.Item().Width(150).Image(firma.LogoData);
                    }
                    else
                    {
                        col.Item().Text(firma.BusinessName ?? firma.OwnerFullName)
                            .FontSize(16).Bold();
                    }
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("ANGEBOT").FontSize(20).Bold().FontColor("#0F2A3D");
                    col.Item().Text($"Nr. {angebot.Angebotsnummer}").FontSize(12);
                });
            });

            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#E5E7EB");

            // Absender und Empfänger
            column.Item().Row(row =>
            {
                // Absender
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(firma.BusinessName ?? firma.OwnerFullName).Bold();
                    col.Item().Text(firma.Address);
                    col.Item().Text($"{firma.PostalCode} {firma.City}");
                    if (!string.IsNullOrEmpty(firma.Phone))
                        col.Item().Text($"Tel: {firma.Phone}");
                    if (!string.IsNullOrEmpty(firma.Email))
                        col.Item().Text($"E-Mail: {firma.Email}");
                });

                // Empfänger
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("An:").FontSize(8).FontColor("#6B7280");
                    col.Item().PaddingTop(5);
                    col.Item().Text(kunde.Name).Bold();
                    col.Item().Text(kunde.Address);
                    col.Item().Text($"{kunde.PostalCode} {kunde.City}");
                    col.Item().Text(kunde.Country);
                });
            });

            column.Item().PaddingVertical(15);

            // Angebotsdaten
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Datum: {angebot.Erstelldatum:dd.MM.yyyy}");
                    col.Item().Text($"Gültig bis: {angebot.GueltigBis:dd.MM.yyyy}");
                    if (!string.IsNullOrEmpty(angebot.Referenz))
                        col.Item().Text($"Referenz: {angebot.Referenz}");
                });
            });

            column.Item().PaddingVertical(10);

            // Einleitung
            if (!string.IsNullOrEmpty(angebot.Einleitung))
            {
                column.Item().Text(angebot.Einleitung);
                column.Item().PaddingVertical(10);
            }
        });
    }

    private void RenderContent(IContainer container, Angebot angebot, Company firma)
    {
        container.Column(column =>
        {
            // Positionstabelle
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Pos
                    columns.RelativeColumn(4);   // Beschreibung
                    columns.ConstantColumn(60);  // Menge
                    columns.ConstantColumn(80);  // Einzelpreis
                    columns.ConstantColumn(50);  // Rabatt
                    columns.ConstantColumn(80);  // Gesamt
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background("#0F2A3D").Padding(5)
                        .Text("Pos").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#0F2A3D").Padding(5)
                        .Text("Beschreibung").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#0F2A3D").Padding(5).AlignRight()
                        .Text("Menge").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#0F2A3D").Padding(5).AlignRight()
                        .Text("Einzelpreis").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#0F2A3D").Padding(5).AlignRight()
                        .Text("Rabatt").FontColor("#FFFFFF").Bold();
                    header.Cell().Background("#0F2A3D").Padding(5).AlignRight()
                        .Text("Gesamt").FontColor("#FFFFFF").Bold();
                });

                // Positionen
                var posNr = 1;
                foreach (var pos in angebot.Positionen.OrderBy(p => p.Position))
                {
                    var bgColor = posNr % 2 == 0 ? "#F9FAFB" : "#FFFFFF";

                    table.Cell().Background(bgColor).Padding(5)
                        .Text(posNr.ToString());
                    table.Cell().Background(bgColor).Padding(5)
                        .Text(pos.Text);
                    table.Cell().Background(bgColor).Padding(5).AlignRight()
                        .Text(pos.Menge.ToString("N2"));
                    table.Cell().Background(bgColor).Padding(5).AlignRight()
                        .Text($"{pos.Einzelpreis:N2} €");
                    table.Cell().Background(bgColor).Padding(5).AlignRight()
                        .Text(pos.Rabatt.HasValue ? $"{pos.Rabatt:N1}%" : "-");
                    table.Cell().Background(bgColor).Padding(5).AlignRight()
                        .Text($"{pos.Nettosumme:N2} €");

                    posNr++;
                }
            });

            column.Item().PaddingVertical(10);

            // Summenblock
            column.Item().AlignRight().Width(250).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(100);
                });

                table.Cell().Padding(3).Text("Nettosumme:");
                table.Cell().Padding(3).AlignRight().Text($"{angebot.Nettosumme:N2} €");

                // MwSt nach Sätzen gruppiert
                var steuersaetze = angebot.Positionen
                    .GroupBy(p => p.Steuersatz)
                    .Select(g => new { Satz = g.Key, Betrag = g.Sum(p => p.Steuerbetrag) });

                foreach (var steuer in steuersaetze)
                {
                    table.Cell().Padding(3).Text($"MwSt {steuer.Satz:N1}%:");
                    table.Cell().Padding(3).AlignRight().Text($"{steuer.Betrag:N2} €");
                }

                // Kleinunternehmerregelung Hinweis
                if (firma.IsKleinunternehmer)
                {
                    table.Cell().ColumnSpan(2).Padding(3)
                        .Text("Gemäß § 19 UStG wird keine Umsatzsteuer berechnet.")
                        .FontSize(8).FontColor("#6B7280");
                }

                table.Cell().BorderTop(1).BorderColor("#1A1A1A").Padding(3).Text("Gesamtbetrag:").Bold();
                table.Cell().BorderTop(1).BorderColor("#1A1A1A").Padding(3).AlignRight()
                    .Text($"{angebot.Bruttosumme:N2} €").Bold();
            });

            column.Item().PaddingVertical(15);

            // Schlusstext
            if (!string.IsNullOrEmpty(angebot.Schlusstext))
            {
                column.Item().Text(angebot.Schlusstext);
            }
            else
            {
                column.Item().Text("Wir freuen uns auf Ihre Rückmeldung und stehen für Rückfragen gerne zur Verfügung.");
            }
        });
    }

    private void RenderFooter(IContainer container, Angebot angebot, Company firma)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor("#E5E7EB");
            column.Item().PaddingTop(5);

            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Dieses Angebot ist gültig bis zum {angebot.GueltigBis:dd.MM.yyyy}.")
                        .FontSize(8).FontColor("#6B7280");
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.Span("Seite ").FontSize(8).FontColor("#6B7280");
                        text.CurrentPageNumber().FontSize(8).FontColor("#6B7280");
                        text.Span(" von ").FontSize(8).FontColor("#6B7280");
                        text.TotalPages().FontSize(8).FontColor("#6B7280");
                    });
                });
            });

            if (!string.IsNullOrEmpty(firma.FooterText))
            {
                column.Item().PaddingTop(5).Text(firma.FooterText).FontSize(8).FontColor("#6B7280");
            }
        });
    }
}
