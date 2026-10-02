using System.Globalization;
using System.Text;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public static class OpenItemsCsvWriter
{
    private const string Separator = ";";
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    public static byte[] Write(IEnumerable<OpenItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separator, "Rechnungsdatum", "Kundenname", "Fälligkeitsdatum", "Brutto", "Gezahlt", "Offener Betrag"));

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(Separator,
                FormatDate(item.InvoiceDate),
                Escape(item.CustomerName),
                item.DueDate.HasValue ? FormatDate(item.DueDate.Value) : string.Empty,
                FormatAmount(item.TotalGross),
                FormatAmount(item.TotalPaid),
                FormatAmount(item.OpenAmount)));
        }

        // BOM, damit Excel die Umlaute korrekt als UTF-8 erkennt
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(sb.ToString())];
    }

    public static string FileName(DateTime from, DateTime to)
        => $"OP-Liste_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.csv";

    private static string FormatDate(DateTime date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    private static string FormatAmount(decimal amount) => amount.ToString("F2", GermanCulture);

    private static string Escape(string value)
    {
        if (value.IndexOfAny([';', '"', '\n', '\r']) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
