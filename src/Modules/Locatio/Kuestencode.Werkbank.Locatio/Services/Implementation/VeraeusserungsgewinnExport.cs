using System.Globalization;
using System.Text;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Kuestencode.Werkbank.Locatio.Services;

/// <summary>
/// Export des Veräußerungsgewinn-Überschlags für den Steuerberater.
/// </summary>
public static class VeraeusserungsgewinnExport
{
    public const string Hinweis =
        "Orientierungswert, keine Steuerberechnung. Die steuerliche Beurteilung und Berechnung erfolgt durch den Steuerberater.";

    private static readonly CultureInfo De = new("de-DE");

    public static IReadOnlyList<(string Bezeichnung, string Wert)> Zeilen(VeraeusserungsgewinnDto dto) =>
    [
        ("Objekt", dto.Objekt),
        ("Anschaffungsdatum", dto.AnschaffungsDatum?.ToString("dd.MM.yyyy", De) ?? "–"),
        ("Verkaufsdatum", dto.VerkaufsDatum?.ToString("dd.MM.yyyy", De) ?? "–"),
        ("Haltedauer", HaltedauerText(dto.HaltedauerMonate)),
        ("Veräußerungspreis", Euro(dto.Veraeusserungspreis)),
        ("./. Veräußerungskosten", Euro(dto.Veraeusserungskosten)),
        ("Anschaffungskosten", Euro(dto.Anschaffungskosten)),
        ("./. kumulierte AfA", Euro(dto.KumulierteAfa)),
        ("= Restbuchwert", Euro(dto.Restbuchwert)),
        ("Überschlag Veräußerungsgewinn", Euro(dto.Gewinn))
    ];

    public static string HaltedauerText(int? monate) =>
        monate is { } m ? $"{m / 12} Jahre, {m % 12} Monate" : "unbekannt (Anschaffungs- oder Verkaufsdatum fehlt)";

    public static byte[] ErzeugeCsv(VeraeusserungsgewinnDto dto)
    {
        var csv = new StringBuilder();
        csv.AppendLine("Bezeichnung;Wert");
        foreach (var (bezeichnung, wert) in Zeilen(dto))
            csv.AppendLine($"{bezeichnung};{wert.Replace(";", ",")}");
        if (dto.UnterZehnJahren)
            csv.AppendLine("Hinweis;Haltedauer unter 10 Jahren (§ 23 EStG)");
        csv.AppendLine($"Hinweis;{Hinweis}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public static byte[] ErzeugePdf(VeraeusserungsgewinnDto dto, string eigentuemer) =>
        Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Text(eigentuemer).FontSize(12).SemiBold();
                col.Item().Text("Überschlag Veräußerungsgewinn").FontSize(18).Bold();
                col.Item().Text($"Erstellt am {DateTime.Now:dd.MM.yyyy}").FontColor(Colors.Grey.Darken1);
            });

            page.Content().PaddingTop(20).Column(col =>
            {
                col.Item().Background(Colors.Orange.Lighten4).Padding(8).Text(Hinweis).Italic();

                if (dto.UnterZehnJahren)
                    col.Item().PaddingTop(8).Background(Colors.Red.Lighten4).Padding(8)
                        .Text("Haltedauer unter 10 Jahren: Der Verkauf kann ein steuerpflichtiges privates Veräußerungsgeschäft (§ 23 EStG) sein.");

                col.Item().PaddingTop(16).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                    });

                    foreach (var (bezeichnung, wert) in Zeilen(dto))
                    {
                        var hervorheben = bezeichnung.StartsWith("Überschlag");
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4)
                            .Text(t => { var s = t.Span(bezeichnung); if (hervorheben) s.Bold(); });
                        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).AlignRight()
                            .Text(t => { var s = t.Span(wert); if (hervorheben) s.Bold(); });
                    }
                });
            });

            page.Footer().AlignCenter().Text(Hinweis).FontSize(8).FontColor(Colors.Grey.Darken1);
        })).GeneratePdf();

    private static string Euro(decimal betrag) => betrag.ToString("N2", De) + " €";
}
