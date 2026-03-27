using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Kuestencode.Werkbank.Saldo.Services;

public class PdfReportService : IPdfReportService
{
    private readonly IEuerService _euerService;
    private readonly ICompanyService _companyService;
    private readonly ILogger<PdfReportService> _logger;

    private const string ColorFallbackPrimary = "#1a365d";
    private const string ColorGreen           = "#2E7D32";
    private const string ColorRed             = "#C62828";
    private const string ColorGrayLight       = "#F5F5F5";
    private const string ColorGrayMid         = "#E0E0E0";
    private const string ColorText            = "#1a1a1a";
    private const string ColorTextMuted       = "#666666";

    public PdfReportService(
        IEuerService euerService,
        ICompanyService companyService,
        ILogger<PdfReportService> logger)
    {
        _euerService    = euerService;
        _companyService = companyService;
        _logger         = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateEuerReportAsync(DateOnly von, DateOnly bis)
    {
        var summary = await _euerService.GetEuerSummaryAsync(new EuerFilterDto { Von = von, Bis = bis });

        Company? company = null;
        try { company = await _companyService.GetCompanyAsync(); } catch { }

        var primaryColor = ParseColor(company?.PdfPrimaryColor) ?? ColorFallbackPrimary;
        var primaryLight = MixWithWhite(primaryColor, 0.10);  // 10% opacity approximation
        var primaryMuted = MixWithWhite(primaryColor, 0.50);  // 50% opacity approximation

        var firmaName       = company?.DisplayName ?? string.Empty;
        var firmaAdresse    = company != null ? $"{company.Address}, {company.PostalCode} {company.City}" : string.Empty;
        var steuernummer    = company?.TaxNumber ?? string.Empty;
        var ustId           = company?.VatId ?? string.Empty;
        var geschaeftsfuehrer = company?.OwnerFullName ?? string.Empty;
        var logoBytes    = company?.LogoData;
        var zeitraum     = $"{von:dd.MM.yyyy} – {bis:dd.MM.yyyy}";
        var erstelltAm   = DateTime.Now;

        var document = Document.Create(container =>
        {
            // ─── Seite 1: Deckblatt ─────────────────────────────────────────────
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(ColorText));

                page.Content().Column(col =>
                {
                    // Header-Banner in Primärfarbe
                    col.Item().Background(primaryColor).Padding(40).Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            if (logoBytes is { Length: > 0 })
                            {
                                row.ConstantItem(80).Height(60).Image(logoBytes).FitArea();
                                row.ConstantItem(20);
                            }
                            row.RelativeItem().AlignMiddle().Text(firmaName)
                                .FontSize(20).Bold().FontColor(Colors.White);
                        });
                    });

                    // Weißer Bereich mit Titel
                    col.Item().PaddingHorizontal(40).PaddingTop(60).Column(body =>
                    {
                        body.Item().Text("Einnahmen-Überschuss-Rechnung")
                            .FontSize(28).Bold().FontColor(primaryColor);
                        body.Item().PaddingTop(8)
                            .Text($"Geschäftsjahr {von.Year}{(von.Year != bis.Year ? "–" + bis.Year : "")}")
                            .FontSize(16).FontColor(ColorTextMuted);

                        if (summary.IstKleinunternehmer)
                        {
                            body.Item().PaddingTop(16)
                                .Background("#FFF8E1").Border(1).BorderColor("#FFD54F")
                                .Padding(10)
                                .Text("Kleinunternehmer nach §19 UStG – keine Umsatzsteuerausweisung")
                                .FontSize(9).FontColor("#795548").Italic();
                        }

                        body.Item().PaddingTop(60).LineHorizontal(1).LineColor(ColorGrayMid);

                        body.Item().PaddingTop(30).Column(info =>
                        {
                            if (!string.IsNullOrEmpty(firmaName))
                                InfoRow(info, "Firma", firmaName);
                            if (!string.IsNullOrEmpty(geschaeftsfuehrer) && geschaeftsfuehrer != firmaName)
                                InfoRow(info, "Inhaber / GF", geschaeftsfuehrer);
                            if (!string.IsNullOrEmpty(firmaAdresse))
                                InfoRow(info, "Adresse", firmaAdresse);
                            if (!string.IsNullOrEmpty(steuernummer))
                                InfoRow(info, "Steuernummer", steuernummer);
                            if (!string.IsNullOrEmpty(ustId))
                                InfoRow(info, "USt-IdNr.", ustId);
                            info.Item().PaddingTop(10);
                            InfoRow(info, "Zeitraum", zeitraum);
                            InfoRow(info, "Erstellt am", erstelltAm.ToString("dd.MM.yyyy"));
                        });
                    });

                    // Footer Deckblatt – kein Werkbank-Branding
                    col.Item().Extend().AlignBottom()
                        .PaddingHorizontal(40).PaddingBottom(20)
                        .Text("Einnahmen-Überschuss-Rechnung – vertraulich")
                        .FontSize(8).FontColor(ColorTextMuted).Italic();
                });
            });

            // ─── Seite 2: Zusammenfassung ────────────────────────────────────────
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Element(c => PageHeader(c, "Zusammenfassung", zeitraum, logoBytes, firmaName, primaryColor, primaryMuted));
                page.Footer().Element(PageFooter);

                page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                {
                    // Drei Kacheln
                    col.Item().Row(row =>
                    {
                        SummaryTile(row.RelativeItem(), "Betriebseinnahmen (Netto)", summary.EinnahmenNetto, ColorGreen, primaryLight);
                        row.ConstantItem(10);
                        SummaryTile(row.RelativeItem(), "Betriebsausgaben (Netto)", summary.AusgabenNetto, ColorRed, primaryLight);
                        row.ConstantItem(10);
                        var gewinn = summary.Ueberschuss;
                        SummaryTile(row.RelativeItem(),
                            gewinn >= 0 ? "Gewinn (Überschuss)" : "Verlust",
                            gewinn, gewinn >= 0 ? ColorGreen : ColorRed, primaryLight);
                    });

                    col.Item().PaddingTop(24);

                    // USt-Block
                    if (!summary.IstKleinunternehmer)
                    {
                        col.Item().Background(ColorGrayLight).Border(1).BorderColor(ColorGrayMid)
                            .Padding(16).Column(ust =>
                            {
                                ust.Item().Text("Umsatzsteuer-Übersicht")
                                    .FontSize(12).Bold().FontColor(primaryColor);
                                ust.Item().PaddingTop(10).Table(table =>
                                {
                                    table.ColumnsDefinition(cols =>
                                    {
                                        cols.RelativeColumn();
                                        cols.ConstantColumn(120);
                                    });

                                    UstRow(table, "Vereinnahmte Umsatzsteuer", summary.EinnahmenMwst, false, primaryColor);
                                    UstRow(table, "Abziehbare Vorsteuer",       summary.AusgabenMwst,  false, primaryColor);

                                    var zahllast = summary.EinnahmenMwst - summary.AusgabenMwst;
                                    UstRow(table, zahllast >= 0 ? "USt-Zahllast" : "USt-Erstattung",
                                        zahllast, true, primaryColor);
                                });
                            });

                        col.Item().PaddingTop(24);
                    }

                    // Gesamtübersicht
                    col.Item().Text("Gesamtübersicht").FontSize(12).Bold().FontColor(primaryColor);
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(90);
                            cols.ConstantColumn(90);
                            cols.ConstantColumn(90);
                        });

                        table.Header(h =>
                        {
                            TableHeaderCell(h.Cell(), "Position", primaryColor);
                            TableHeaderCell(h.Cell(), "Netto", primaryColor, true);
                            TableHeaderCell(h.Cell(), "MwSt", primaryColor, true);
                            TableHeaderCell(h.Cell(), "Brutto", primaryColor, true);
                        });

                        SummaryTableRow(table, "Betriebseinnahmen",
                            summary.EinnahmenNetto, summary.EinnahmenMwst, summary.EinnahmenBrutto,
                            ColorGreen, false, primaryColor);
                        SummaryTableRow(table, "Betriebsausgaben",
                            summary.AusgabenNetto, summary.AusgabenMwst, summary.AusgabenBrutto,
                            ColorRed, false, primaryColor);
                        SummaryTableRow(table,
                            summary.Ueberschuss >= 0 ? "Überschuss (Gewinn)" : "Fehlbetrag (Verlust)",
                            summary.Ueberschuss, 0, 0,
                            summary.Ueberschuss >= 0 ? ColorGreen : ColorRed, true, primaryColor);
                    });
                });
            });

            // ─── Seite 3+: Aufstellung Einnahmen ────────────────────────────────
            if (summary.Einnahmen.Any())
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => PageHeader(c, "Betriebseinnahmen", zeitraum, logoBytes, firmaName, primaryColor, primaryMuted));
                    page.Footer().Element(PageFooter);

                    page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                    {
                        col.Item().Text("Einnahmen nach Erlöskonto")
                            .FontSize(11).Bold().FontColor(primaryColor);
                        col.Item().PaddingTop(8)
                            .Element(c => PositionenTabelle(c, summary.Einnahmen, ColorGreen, primaryColor, primaryLight));
                    });
                });
            }

            // ─── Seite X+: Aufstellung Ausgaben ─────────────────────────────────
            if (summary.Ausgaben.Any())
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => PageHeader(c, "Betriebsausgaben", zeitraum, logoBytes, firmaName, primaryColor, primaryMuted));
                    page.Footer().Element(PageFooter);

                    page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                    {
                        col.Item().Text("Ausgaben nach Kategorie / Konto")
                            .FontSize(11).Bold().FontColor(primaryColor);
                        col.Item().PaddingTop(8)
                            .Element(c => PositionenTabelle(c, summary.Ausgaben, ColorRed, primaryColor, primaryLight, showAnteil: true));
                    });
                });
            }

        });

        return document.GeneratePdf();
    }

    public async Task<string> GetEuerReportFileNameAsync(DateOnly von, DateOnly bis)
    {
        Company? company = null;
        try { company = await _companyService.GetCompanyAsync(); } catch { }

        var safeCompanyName = SanitizeFileName(company?.DisplayName ?? "");
        var suffix = string.IsNullOrEmpty(safeCompanyName) ? "" : "_" + safeCompanyName;
        return $"EÜR_{von.Year}{suffix}.pdf";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var safe = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return safe.Replace(' ', '_').Trim('_');
    }

    // ─── Hilfs-Methoden ─────────────────────────────────────────────────────────

    private static readonly System.Globalization.CultureInfo _culture =
        System.Globalization.CultureInfo.GetCultureInfo("de-DE");

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(0);
        page.MarginTop(0);
        page.MarginBottom(20);
        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(ColorText));
    }

    private static void PageHeader(IContainer container, string section, string zeitraum,
        byte[]? logoBytes, string firmaName, string primaryColor, string primaryMuted)
    {
        container.Background(primaryColor).PaddingHorizontal(40).PaddingVertical(12).Row(row =>
        {
            // Links: Logo oder Firmenname (klein)
            if (logoBytes is { Length: > 0 })
                row.ConstantItem(80).Height(36).Image(logoBytes).FitArea();
            else
                row.ConstantItem(160).AlignMiddle()
                    .Text(firmaName).FontSize(9).FontColor(Colors.White);

            row.RelativeItem();

            // Rechts: EÜR + Abschnitt
            row.ConstantItem(180).AlignRight().AlignMiddle().Column(c =>
            {
                c.Item().AlignRight().Text($"EÜR – {section}")
                    .FontSize(11).Bold().FontColor(Colors.White);
                c.Item().AlignRight().Text(zeitraum)
                    .FontSize(8).FontColor(primaryMuted);
            });
        });
    }

    private static void PageFooter(IContainer container)
    {
        container.PaddingHorizontal(40).Row(row =>
        {
            row.RelativeItem()
                .Text("Einnahmen-Überschuss-Rechnung – vertraulich")
                .FontSize(8).FontColor(ColorTextMuted).Italic();
            row.ConstantItem(80).AlignRight().Text(x =>
            {
                x.Span("Seite ").FontSize(8).FontColor(ColorTextMuted);
                x.CurrentPageNumber().FontSize(8).FontColor(ColorTextMuted);
                x.Span(" / ").FontSize(8).FontColor(ColorTextMuted);
                x.TotalPages().FontSize(8).FontColor(ColorTextMuted);
            });
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(6).Row(row =>
        {
            row.ConstantItem(150).Text(label + ":").Bold().FontColor(ColorTextMuted);
            row.RelativeItem().Text(value);
        });
    }

    private static void SummaryTile(IContainer container, string label, decimal value, string color, string bgColor)
    {
        container.Background(Colors.White).Border(1).BorderColor("#CCCCCC").BorderLeft(4).BorderColor(color)
            .Padding(14).Column(col =>
            {
                col.Item().Text(label).FontSize(9).FontColor(ColorTextMuted);
                col.Item().PaddingTop(6)
                    .Text(FormatEuro(value))
                    .FontSize(18).Bold().FontColor(color);
            });
    }

    private static void UstRow(TableDescriptor table, string label, decimal value, bool isSumme, string primaryColor)
    {
        var border = isSumme ? (float)1 : 0;
        var valueColor = value >= 0 ? ColorText : ColorGreen;

        table.Cell().BorderTop(border).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6)
            .Text(t => { var s = t.Span(label).FontSize(10); if (isSumme) s.Bold(); });
        table.Cell().BorderTop(border).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t => { var s = t.Span(FormatEuro(value)).FontSize(10).FontColor(valueColor); if (isSumme) s.Bold(); });
    }

    private static void SummaryTableRow(TableDescriptor table,
        string label, decimal netto, decimal mwst, decimal brutto,
        string color, bool isSumme, string primaryColor)
    {
        var topBorder = isSumme ? (float)1 : 0;

        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6)
            .Text(t => { var s = t.Span(label).FontSize(10); if (isSumme) s.Bold(); });
        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t => { var s = t.Span(FormatEuro(netto)).FontSize(10).FontColor(color); if (isSumme) s.Bold(); });
        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t => { var s = t.Span(mwst != 0 ? FormatEuro(mwst) : "–").FontSize(10).FontColor(ColorTextMuted); if (isSumme) s.Bold(); });
        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t => { var s = t.Span(brutto != 0 ? FormatEuro(brutto) : "–").FontSize(10).FontColor(color); if (isSumme) s.Bold(); });
    }

    private static void TableHeaderCell(IContainer cell, string text, string primaryColor, bool alignRight = false)
    {
        var t = cell.Background(primaryColor).PaddingVertical(6).PaddingHorizontal(6);
        if (alignRight)
            t.AlignRight().Text(text).FontSize(9).Bold().FontColor(Colors.White);
        else
            t.Text(text).FontSize(9).Bold().FontColor(Colors.White);
    }

    private static void PositionenTabelle(IContainer container, List<EuerPositionDto> positionen,
        string sumColor, string primaryColor, string primaryLight, bool showAnteil = false)
    {
        var totalNetto  = positionen.Sum(p => p.BetragNetto);

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(60);
                cols.RelativeColumn(3);
                cols.ConstantColumn(90);
                cols.ConstantColumn(80);
                cols.ConstantColumn(90);
                if (showAnteil) cols.ConstantColumn(48);
                cols.ConstantColumn(30);
            });

            table.Header(h =>
            {
                TableHeaderCell(h.Cell(), "Konto", primaryColor);
                TableHeaderCell(h.Cell(), "Bezeichnung", primaryColor);
                TableHeaderCell(h.Cell(), "Netto", primaryColor, true);
                TableHeaderCell(h.Cell(), "MwSt", primaryColor, true);
                TableHeaderCell(h.Cell(), "Brutto", primaryColor, true);
                if (showAnteil) TableHeaderCell(h.Cell(), "Anteil", primaryColor, true);
                TableHeaderCell(h.Cell(), "Bel.", primaryColor, true);
            });

            var alternating = false;
            foreach (var pos in positionen)
            {
                var bg = alternating ? "#FFFFFF" : ColorGrayLight;
                alternating = !alternating;

                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .Text(pos.KontoNummer).FontSize(9).FontColor(ColorTextMuted);
                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .Column(c =>
                    {
                        c.Item().Text(pos.KontoBezeichnung).FontSize(9);
                        if (!string.IsNullOrEmpty(pos.Gruppe))
                            c.Item().Text(pos.Gruppe).FontSize(8).FontColor(ColorTextMuted).Italic();
                    });
                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .AlignRight().Text(FormatEuro(pos.BetragNetto)).FontSize(9);
                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .AlignRight().Text(FormatEuro(pos.MwstBetrag)).FontSize(9).FontColor(ColorTextMuted);
                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .AlignRight().Text(FormatEuro(pos.BetragBrutto)).FontSize(9);
                if (showAnteil)
                {
                    var anteil = totalNetto != 0
                        ? (pos.BetragNetto / totalNetto * 100).ToString("N1", _culture) + " %"
                        : "–";
                    table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                        .AlignRight().Text(anteil).FontSize(9).FontColor(ColorTextMuted);
                }
                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .AlignRight().Text(pos.AnzahlBelege.ToString()).FontSize(9).FontColor(ColorTextMuted);
            }

            var totalMwst   = positionen.Sum(p => p.MwstBetrag);
            var totalBrutto = positionen.Sum(p => p.BetragBrutto);
            var totalBelege = positionen.Sum(p => p.AnzahlBelege);

            table.Cell().ColumnSpan(2)
                .BorderTop(1).BorderColor(primaryColor)
                .PaddingVertical(6).PaddingHorizontal(6)
                .Text("Gesamt").Bold().FontSize(10);
            table.Cell().BorderTop(1).BorderColor(primaryColor)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(FormatEuro(totalNetto)).Bold().FontSize(10).FontColor(sumColor);
            table.Cell().BorderTop(1).BorderColor(primaryColor)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(FormatEuro(totalMwst)).Bold().FontSize(10).FontColor(ColorTextMuted);
            table.Cell().BorderTop(1).BorderColor(primaryColor)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(FormatEuro(totalBrutto)).Bold().FontSize(10).FontColor(sumColor);
            if (showAnteil)
            {
                table.Cell().BorderTop(1).BorderColor(primaryColor)
                    .PaddingVertical(6).PaddingHorizontal(6);
            }
            table.Cell().BorderTop(1).BorderColor(primaryColor)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(totalBelege.ToString()).Bold().FontSize(10).FontColor(ColorTextMuted);
        });
    }

    // ─── Farb-Hilfsfunktionen ────────────────────────────────────────────────────

    /// <summary>Parses a hex color string and returns it if valid, null otherwise.</summary>
    private static string? ParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return null;
        try
        {
            Convert.ToByte(hex.Substring(0, 2), 16);
            Convert.ToByte(hex.Substring(2, 2), 16);
            Convert.ToByte(hex.Substring(4, 2), 16);
            return "#" + hex;
        }
        catch { return null; }
    }

    /// <summary>
    /// Blends a hex color toward white by <paramref name="whiteFraction"/> (0=original, 1=white).
    /// Returns the blended hex color.
    /// </summary>
    private static string MixWithWhite(string hex, double whiteFraction)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6) return "#FFFFFF";
        try
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            var nr = (byte)(r + (255 - r) * whiteFraction);
            var ng = (byte)(g + (255 - g) * whiteFraction);
            var nb = (byte)(b + (255 - b) * whiteFraction);
            return $"#{nr:X2}{ng:X2}{nb:X2}";
        }
        catch { return "#FFFFFF"; }
    }

    private static string FormatEuro(decimal value)
        => value.ToString("N2", _culture) + " €";
}
