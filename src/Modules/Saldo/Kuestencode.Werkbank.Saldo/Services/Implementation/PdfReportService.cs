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

    // Werkbank-Farben
    private static readonly string ColorPrimary    = "#1565C0"; // dunkelblau
    private static readonly string ColorAccent     = "#1976D2";
    private static readonly string ColorGreen      = "#2E7D32";
    private static readonly string ColorRed        = "#C62828";
    private static readonly string ColorGrayLight  = "#F5F5F5";
    private static readonly string ColorGrayMid    = "#E0E0E0";
    private static readonly string ColorText       = "#212121";
    private static readonly string ColorTextMuted  = "#757575";

    public PdfReportService(
        IEuerService euerService,
        ICompanyService companyService,
        ILogger<PdfReportService> logger)
    {
        _euerService = euerService;
        _companyService = companyService;
        _logger = logger;

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GenerateEuerReportAsync(DateOnly von, DateOnly bis)
    {
        var summary = await _euerService.GetEuerSummaryAsync(new EuerFilterDto { Von = von, Bis = bis });
        Company? company = null;
        try { company = await _companyService.GetCompanyAsync(); } catch { }

        var firmaName = company?.DisplayName ?? "Unbekannte Firma";
        var firmaAdresse = company != null
            ? $"{company.Address}, {company.PostalCode} {company.City}"
            : string.Empty;
        var steuernummer = company?.TaxNumber ?? string.Empty;

        var erstelltAm = DateTime.Now;
        var zeitraum = $"{von:dd.MM.yyyy} – {bis:dd.MM.yyyy}";

        byte[]? logoBytes = company?.LogoData;

        var document = Document.Create(container =>
        {
            // ─── Seite 1: Deckblatt ──────────────────────────────────────────
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor(ColorText));

                page.Content().Column(col =>
                {
                    // Blauer Header-Banner
                    col.Item().Background(ColorPrimary).Padding(40).Column(header =>
                    {
                        // Logo + Firmenname nebeneinander
                        header.Item().Row(row =>
                        {
                            if (logoBytes != null && logoBytes.Length > 0)
                            {
                                row.ConstantItem(80).Height(60).Image(logoBytes).FitArea();
                                row.ConstantItem(20); // Abstand
                            }
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(firmaName)
                                    .FontSize(18).Bold().FontColor(Colors.White);
                                if (!string.IsNullOrEmpty(firmaAdresse))
                                    c.Item().Text(firmaAdresse)
                                        .FontSize(10).FontColor("#BBDEFB");
                                if (!string.IsNullOrEmpty(steuernummer))
                                    c.Item().Text($"St.-Nr.: {steuernummer}")
                                        .FontSize(10).FontColor("#BBDEFB");
                            });
                        });
                    });

                    // Weißer Bereich mit Titel
                    col.Item().PaddingHorizontal(40).PaddingTop(60).Column(body =>
                    {
                        body.Item().Text("Einnahmen-Überschuss-Rechnung")
                            .FontSize(28).Bold().FontColor(ColorPrimary);
                        body.Item().PaddingTop(8).Text(zeitraum)
                            .FontSize(16).FontColor(ColorTextMuted);

                        if (summary.IstKleinunternehmer)
                        {
                            body.Item().PaddingTop(16).Background("#FFF8E1").Border(1).BorderColor("#FFD54F")
                                .Padding(10).Text("Kleinunternehmer nach §19 UStG – keine Umsatzsteuerausweisung")
                                .FontSize(9).FontColor("#795548").Italic();
                        }

                        body.Item().PaddingTop(60).LineHorizontal(1).LineColor(ColorGrayMid);

                        body.Item().PaddingTop(30).Column(info =>
                        {
                            InfoRow(info, "Firma", firmaName);
                            if (!string.IsNullOrEmpty(steuernummer))
                                InfoRow(info, "Steuernummer", steuernummer);
                            InfoRow(info, "Zeitraum", zeitraum);
                            InfoRow(info, "Erstellt am", erstelltAm.ToString("dd.MM.yyyy HH:mm") + " Uhr");
                        });
                    });

                    // Footer Deckblatt
                    col.Item().Extend().AlignBottom().PaddingHorizontal(40).PaddingBottom(20)
                        .Text("Küstencode Werkbank – automatisch generierter Bericht")
                        .FontSize(8).FontColor(ColorTextMuted).Italic();
                });
            });

            // ─── Seite 2: Zusammenfassung ────────────────────────────────────
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Element(c => PageHeader(c, "Zusammenfassung", zeitraum));
                page.Footer().Element(PageFooter);

                page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                {
                    // Drei Kacheln: Einnahmen / Ausgaben / Gewinn
                    col.Item().Row(row =>
                    {
                        SummaryTile(row.RelativeItem(), "Betriebseinnahmen (Netto)",
                            summary.EinnahmenNetto, ColorGreen);
                        row.ConstantItem(10);
                        SummaryTile(row.RelativeItem(), "Betriebsausgaben (Netto)",
                            summary.AusgabenNetto, ColorRed);
                        row.ConstantItem(10);
                        var gewinn = summary.Ueberschuss;
                        SummaryTile(row.RelativeItem(),
                            gewinn >= 0 ? "Gewinn (Überschuss)" : "Verlust",
                            gewinn, gewinn >= 0 ? ColorGreen : ColorRed);
                    });

                    col.Item().PaddingTop(24);

                    // USt-Block
                    col.Item().Background(ColorGrayLight).Border(1).BorderColor(ColorGrayMid)
                        .Padding(16).Column(ust =>
                    {
                        ust.Item().Text("Umsatzsteuer-Übersicht")
                            .FontSize(12).Bold().FontColor(ColorPrimary);
                        ust.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.ConstantColumn(120);
                            });

                            UstRow(table, "Umsatzsteuer (aus Einnahmen)",  summary.EinnahmenMwst, false);
                            UstRow(table, "Vorsteuer (aus Ausgaben)",       summary.AusgabenMwst,  false);

                            var zahllast = summary.EinnahmenMwst - summary.AusgabenMwst;
                            UstRow(table,
                                zahllast >= 0 ? "USt-Zahllast" : "USt-Erstattung",
                                zahllast, true);
                        });
                    });

                    col.Item().PaddingTop(24);

                    // Gesamtübersicht als einfache Tabelle
                    col.Item().Text("Gesamtübersicht").FontSize(12).Bold().FontColor(ColorPrimary);
                    col.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.ConstantColumn(90);
                            cols.ConstantColumn(90);
                            cols.ConstantColumn(90);
                        });

                        // Header
                        table.Header(h =>
                        {
                            TableHeaderCell(h.Cell(), "Position");
                            TableHeaderCell(h.Cell(), "Netto", true);
                            TableHeaderCell(h.Cell(), "MwSt", true);
                            TableHeaderCell(h.Cell(), "Brutto", true);
                        });

                        // Einnahmen-Zeile
                        SummaryTableRow(table, "Betriebseinnahmen",
                            summary.EinnahmenNetto, summary.EinnahmenMwst, summary.EinnahmenBrutto,
                            ColorGreen, false);

                        // Ausgaben-Zeile
                        SummaryTableRow(table, "Betriebsausgaben",
                            summary.AusgabenNetto, summary.AusgabenMwst, summary.AusgabenBrutto,
                            ColorRed, false);

                        // Trennlinie + Überschuss
                        SummaryTableRow(table, summary.Ueberschuss >= 0 ? "Überschuss (Gewinn)" : "Fehlbetrag (Verlust)",
                            summary.Ueberschuss, 0, 0,
                            summary.Ueberschuss >= 0 ? ColorGreen : ColorRed, true);
                    });
                });
            });

            // ─── Seite 3+: Aufstellung Einnahmen ─────────────────────────────
            if (summary.Einnahmen.Any())
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => PageHeader(c, "Aufstellung Einnahmen", zeitraum));
                    page.Footer().Element(PageFooter);

                    page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                    {
                        col.Item().Text("Einnahmen nach Erlöskonto")
                            .FontSize(11).Bold().FontColor(ColorPrimary);
                        col.Item().PaddingTop(8).Element(c => PositionenTabelle(c, summary.Einnahmen, ColorGreen));
                    });
                });
            }

            // ─── Seite X+: Aufstellung Ausgaben ──────────────────────────────
            if (summary.Ausgaben.Any())
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => PageHeader(c, "Aufstellung Ausgaben", zeitraum));
                    page.Footer().Element(PageFooter);

                    page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                    {
                        col.Item().Text("Ausgaben nach Kategorie / Konto")
                            .FontSize(11).Bold().FontColor(ColorPrimary);
                        col.Item().PaddingTop(8).Element(c => PositionenTabelle(c, summary.Ausgaben, ColorRed));
                    });
                });
            }

            // ─── Letzte Seite: Ausgaben-Zusammenfassung nach Kategorie ───────
            if (summary.Ausgaben.Any())
            {
                container.Page(page =>
                {
                    ConfigurePage(page);
                    page.Header().Element(c => PageHeader(c, "Ausgaben nach Kategorie", zeitraum));
                    page.Footer().Element(PageFooter);

                    page.Content().PaddingHorizontal(40).PaddingTop(20).Column(col =>
                    {
                        col.Item().Text("Zusammenfassung Betriebsausgaben")
                            .FontSize(11).Bold().FontColor(ColorPrimary);
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(60);
                                cols.RelativeColumn();
                                cols.ConstantColumn(90);
                                cols.ConstantColumn(25);
                            });

                            table.Header(h =>
                            {
                                TableHeaderCell(h.Cell(), "Konto");
                                TableHeaderCell(h.Cell(), "Bezeichnung");
                                TableHeaderCell(h.Cell(), "Netto", true);
                                TableHeaderCell(h.Cell(), "Bel.");
                            });

                            var alternating = false;
                            foreach (var pos in summary.Ausgaben.OrderBy(p => p.KontoNummer))
                            {
                                var bg = alternating ? "#FFFFFF" : ColorGrayLight;
                                alternating = !alternating;

                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                                    .Text(pos.KontoNummer).FontSize(9);
                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                                    .Text(pos.KontoBezeichnung).FontSize(9);
                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                                    .AlignRight().Text(FormatEuro(pos.BetragNetto)).FontSize(9).FontColor(ColorRed);
                                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                                    .AlignRight().Text(pos.AnzahlBelege.ToString()).FontSize(9).FontColor(ColorTextMuted);
                            }

                            // Summenzeile
                            var total = summary.Ausgaben.Sum(p => p.BetragNetto);
                            table.Cell().ColumnSpan(2)
                                .BorderTop(1).BorderColor(ColorPrimary)
                                .PaddingVertical(6).PaddingHorizontal(6)
                                .Text("Gesamt Betriebsausgaben").Bold().FontSize(10);
                            table.Cell()
                                .BorderTop(1).BorderColor(ColorPrimary)
                                .PaddingVertical(6).PaddingHorizontal(6)
                                .AlignRight().Text(FormatEuro(total)).Bold().FontSize(10).FontColor(ColorRed);
                            table.Cell()
                                .BorderTop(1).BorderColor(ColorPrimary)
                                .PaddingVertical(6);
                        });

                        // Überschuss-Box am Ende
                        col.Item().PaddingTop(30).Background(
                                summary.Ueberschuss >= 0
                                    ? "#E8F5E9"  // grün-hell
                                    : "#FFEBEE") // rot-hell
                            .Border(1).BorderColor(summary.Ueberschuss >= 0 ? ColorGreen : ColorRed)
                            .Padding(16).Row(row =>
                            {
                                row.RelativeItem().Text(
                                    summary.Ueberschuss >= 0
                                        ? "Gewinn (Einnahmen – Ausgaben Netto)"
                                        : "Verlust (Ausgaben – Einnahmen Netto)")
                                    .Bold().FontSize(12);
                                row.ConstantItem(160).AlignRight()
                                    .Text(FormatEuro(summary.Ueberschuss))
                                    .Bold().FontSize(14)
                                    .FontColor(summary.Ueberschuss >= 0 ? ColorGreen : ColorRed);
                            });
                    });
                });
            }
        });

        return document.GeneratePdf();
    }

    // ─── Hilfs-Methoden ───────────────────────────────────────────────────────

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.MarginHorizontal(0);
        page.MarginTop(0);
        page.MarginBottom(20);
        page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10).FontColor("#212121"));
    }

    private static void PageHeader(IContainer container, string title, string zeitraum)
    {
        container.Background(ColorPrimary).PaddingHorizontal(40).PaddingVertical(14).Row(row =>
        {
            row.RelativeItem().Column(c =>
            {
                c.Item().Text("EÜR – " + title)
                    .FontSize(14).Bold().FontColor(Colors.White);
                c.Item().Text(zeitraum)
                    .FontSize(9).FontColor("#BBDEFB");
            });
            row.ConstantItem(120).AlignRight().AlignMiddle()
                .Text("Küstencode Werkbank")
                .FontSize(9).FontColor("#BBDEFB").Italic();
        });
    }

    private static void PageFooter(IContainer container)
    {
        container.PaddingHorizontal(40).Row(row =>
        {
            row.RelativeItem()
                .Text("Einnahmen-Überschuss-Rechnung – vertraulich")
                .FontSize(8).FontColor(ColorTextMuted).Italic();
            row.ConstantItem(60).AlignRight()
                .Text(x =>
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

    private static void SummaryTile(IContainer container, string label, decimal value, string color)
    {
        container.Background(ColorGrayLight).Border(1).BorderColor(ColorGrayMid)
            .Padding(14).Column(col =>
            {
                col.Item().Text(label).FontSize(9).FontColor(ColorTextMuted);
                col.Item().PaddingTop(6)
                    .Text(FormatEuro(value))
                    .FontSize(18).Bold().FontColor(color);
            });
    }

    private static void UstRow(TableDescriptor table, string label, decimal value, bool isSumme)
    {
        var border = isSumme ? (float)1 : 0;
        var valueColor = value >= 0 ? ColorText : ColorGreen;

        table.Cell().BorderTop(border).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6)
            .Text(t =>
            {
                var span = t.Span(label).FontSize(10);
                if (isSumme) span.Bold();
            });
        table.Cell().BorderTop(border).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t =>
            {
                var span = t.Span(FormatEuro(value)).FontSize(10).FontColor(valueColor);
                if (isSumme) span.Bold();
            });
    }

    private static void SummaryTableRow(TableDescriptor table,
        string label, decimal netto, decimal mwst, decimal brutto,
        string color, bool isSumme)
    {
        var topBorder = isSumme ? (float)1 : 0;
        var nettoText = netto == 0 && isSumme ? FormatEuro(netto) : (netto != 0 ? FormatEuro(netto) : "–");

        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6)
            .Text(t =>
            {
                var span = t.Span(label).FontSize(10);
                if (isSumme) span.Bold();
            });
        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t =>
            {
                var span = t.Span(nettoText).FontSize(10).FontColor(color);
                if (isSumme) span.Bold();
            });
        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t =>
            {
                var span = t.Span(mwst != 0 ? FormatEuro(mwst) : "–").FontSize(10).FontColor(ColorTextMuted);
                if (isSumme) span.Bold();
            });
        table.Cell().BorderTop(topBorder).BorderColor(ColorGrayMid)
            .PaddingVertical(5).PaddingHorizontal(6).AlignRight()
            .Text(t =>
            {
                var span = t.Span(brutto != 0 ? FormatEuro(brutto) : "–").FontSize(10).FontColor(color);
                if (isSumme) span.Bold();
            });
    }

    private static void TableHeaderCell(IContainer cell, string text, bool alignRight = false)
    {
        var t = cell.Background(ColorPrimary).PaddingVertical(6).PaddingHorizontal(6);
        if (alignRight)
            t.AlignRight().Text(text).FontSize(9).Bold().FontColor(Colors.White);
        else
            t.Text(text).FontSize(9).Bold().FontColor(Colors.White);
    }

    private static void PositionenTabelle(IContainer container, List<EuerPositionDto> positionen, string sumColor)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(60);
                cols.RelativeColumn(3);
                cols.ConstantColumn(90);
                cols.ConstantColumn(80);
                cols.ConstantColumn(90);
                cols.ConstantColumn(30);
            });

            table.Header(h =>
            {
                TableHeaderCell(h.Cell(), "Konto");
                TableHeaderCell(h.Cell(), "Bezeichnung");
                TableHeaderCell(h.Cell(), "Netto", true);
                TableHeaderCell(h.Cell(), "MwSt", true);
                TableHeaderCell(h.Cell(), "Brutto", true);
                TableHeaderCell(h.Cell(), "Bel.", true);
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
                table.Cell().Background(bg).PaddingVertical(5).PaddingHorizontal(6)
                    .AlignRight().Text(pos.AnzahlBelege.ToString()).FontSize(9).FontColor(ColorTextMuted);
            }

            // Summenzeile
            var totalNetto  = positionen.Sum(p => p.BetragNetto);
            var totalMwst   = positionen.Sum(p => p.MwstBetrag);
            var totalBrutto = positionen.Sum(p => p.BetragBrutto);
            var totalBelege = positionen.Sum(p => p.AnzahlBelege);

            table.Cell().ColumnSpan(2)
                .BorderTop(1).BorderColor(ColorPrimary)
                .PaddingVertical(6).PaddingHorizontal(6)
                .Text("Gesamt").Bold().FontSize(10);
            table.Cell().BorderTop(1).BorderColor(ColorPrimary)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(FormatEuro(totalNetto)).Bold().FontSize(10).FontColor(sumColor);
            table.Cell().BorderTop(1).BorderColor(ColorPrimary)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(FormatEuro(totalMwst)).Bold().FontSize(10).FontColor(ColorTextMuted);
            table.Cell().BorderTop(1).BorderColor(ColorPrimary)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(FormatEuro(totalBrutto)).Bold().FontSize(10).FontColor(sumColor);
            table.Cell().BorderTop(1).BorderColor(ColorPrimary)
                .PaddingVertical(6).PaddingHorizontal(6)
                .AlignRight().Text(totalBelege.ToString()).Bold().FontSize(10).FontColor(ColorTextMuted);
        });
    }

    private static string FormatEuro(decimal value)
    {
        return value.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("de-DE")) + " €";
    }
}
