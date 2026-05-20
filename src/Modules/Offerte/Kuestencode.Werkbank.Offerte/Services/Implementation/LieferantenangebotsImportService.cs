using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Domain.Interfaces;

namespace Kuestencode.Werkbank.Offerte.Services;

public partial class LieferantenangebotsImportService : ILieferantenangebotsImportService
{
    private readonly IAngebotRepository _repository;
    private readonly IAngebotsnummernService _nummernService;
    private readonly ILogger<LieferantenangebotsImportService> _logger;
    private readonly IConfiguration _configuration;

    // German decimal: 1.234,56 or 12,50 (requires 2 decimal places = price format)
    [GeneratedRegex(@"\d{1,3}(?:\.\d{3})*,\d{2}|\d+,\d{2}", RegexOptions.Compiled)]
    private static partial Regex GermanDecimalRegex();

    // Any German number (integer or decimal)
    [GeneratedRegex(@"\d{1,3}(?:\.\d{3})*(?:,\d+)?|\d+(?:,\d+)?", RegexOptions.Compiled)]
    private static partial Regex AnyNumberRegex();

    // Leading position number: "1." or "1 " or "Pos. 1 " etc.
    [GeneratedRegex(@"^\s*(?:Pos(?:ition)?\.?\s*)?\d+\.?\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex LeadingPositionRegex();

    // Trailing quantity + optional unit before the price
    [GeneratedRegex(@"\s+\d+(?:[,\.]\d+)?\s*[a-zA-ZäöüÄÖÜ\.]{0,10}\s*$", RegexOptions.Compiled)]
    private static partial Regex TrailingQtyUnitRegex();

    private static readonly HashSet<string> SkipKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bezeichnung", "beschreibung", "menge", "einheit", "einzelpreis",
        "gesamtpreis", "nettobetrag", "mehrwertsteuer", "mwst", "gesamt",
        "zwischensumme", "summe", "netto", "brutto", "steuer", "rabatt",
        "pos", "position", "artikel", "leistung", "seite", "page",
        "angebot", "lieferant", "datum", "kundennummer", "auftragsnummer"
    };

    public LieferantenangebotsImportService(
        IAngebotRepository repository,
        IAngebotsnummernService nummernService,
        ILogger<LieferantenangebotsImportService> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _nummernService = nummernService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<AngebotImportErgebnis> ImportiereAsync(
        Stream pdfStream,
        string dateiName,
        decimal aufschlagProzent,
        int kundeId)
    {
        var ergebnis = new AngebotImportErgebnis();
        var tempDir = Path.Combine(Path.GetTempPath(), $"lieferant_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "input.pdf");
            await using (var fs = File.Create(inputPath))
                await pdfStream.CopyToAsync(fs);

            var text = await ExtractTextAsync(inputPath, tempDir);

            if (string.IsNullOrWhiteSpace(text))
            {
                ergebnis.Fehler = "Der Text konnte nicht aus dem PDF extrahiert werden. " +
                    "Bitte prüfen Sie, ob das PDF lesbar ist und die Tools pdftotext/tesseract verfügbar sind.";
                return ergebnis;
            }

            _logger.LogInformation("PDF-Text extrahiert: {Chars} Zeichen aus '{File}'", text.Length, dateiName);

            var (positionen, warnungen) = ParsePositionen(text, dateiName);
            ergebnis.Warnungen.AddRange(warnungen);

            foreach (var pos in positionen.Where(p => !p.IsHeader))
                pos.Einzelpreis = Math.Round(pos.Einzelpreis * (1 + aufschlagProzent / 100), 2);

            var nummer = await _nummernService.NaechsteNummerAsync();
            var angebotId = Guid.NewGuid();

            var angebot = new Angebot
            {
                Id = angebotId,
                Angebotsnummer = nummer,
                KundeId = kundeId,
                Status = AngebotStatus.Entwurf,
                Erstelldatum = DateTime.UtcNow,
                GueltigBis = DateTime.UtcNow.AddDays(14),
                Referenz = $"Import: {dateiName}",
                Positionen = positionen
            };

            foreach (var pos in angebot.Positionen)
                pos.AngebotId = angebotId;

            await _repository.AddAsync(angebot);

            ergebnis.Erfolgreich = true;
            ergebnis.AngebotId = angebotId;
            ergebnis.Angebotsnummer = nummer;

            _logger.LogInformation("Lieferantenangebot importiert: {Nr}, {Count} Positionen, Aufschlag {Pct}%",
                nummer, positionen.Count(p => !p.IsHeader), aufschlagProzent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Importieren des Lieferantenangebots '{File}'", dateiName);
            ergebnis.Fehler = $"Interner Fehler: {ex.Message}";
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch (Exception ex) { _logger.LogWarning(ex, "Temp-Verzeichnis konnte nicht gelöscht werden"); }
        }

        return ergebnis;
    }

    private async Task<string> ExtractTextAsync(string pdfPath, string tempDir)
    {
        // Try pdftotext first – fast, high quality for digital PDFs
        try
        {
            var (exitCode, stdout, _) = await RunProcessAsync("pdftotext", $"-layout \"{pdfPath}\" -");
            if (exitCode == 0 && stdout.Trim().Length > 50)
            {
                _logger.LogDebug("pdftotext: {Chars} Zeichen extrahiert", stdout.Length);
                return stdout;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "pdftotext nicht verfügbar, versuche OCR-Fallback");
        }

        // Fallback: Tesseract OCR (first page only via pdftoppm)
        try
        {
            var dpi = _configuration.GetValue<int>("Ocr:PdfDpi", 300);
            var lang = _configuration.GetValue<string>("Ocr:Language") ?? "deu";
            var outputPrefix = Path.Combine(tempDir, "page");

            var (ppExit, _, ppErr) = await RunProcessAsync(
                "pdftoppm",
                $"-png -r {dpi} -f 1 -l 1 \"{pdfPath}\" \"{outputPrefix}\"");

            if (ppExit != 0)
            {
                _logger.LogWarning("pdftoppm Fehler: {Err}", ppErr);
                return string.Empty;
            }

            var pngPath = Directory.GetFiles(tempDir, "page-*.png").FirstOrDefault();
            if (pngPath == null) return string.Empty;

            var (tExit, tOut, _) = await RunProcessAsync("tesseract", $"\"{pngPath}\" stdout -l {lang}");
            return tExit == 0 ? tOut.Trim() : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR-Fallback fehlgeschlagen");
            return string.Empty;
        }
    }

    private static (List<Angebotsposition> Positionen, List<ImportWarnung> Warnungen) ParsePositionen(
        string text, string dateiName)
    {
        var positionen = new List<Angebotsposition>();
        var warnungen = new List<ImportWarnung>();
        var posNr = 0;

        foreach (var rawLine in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length < 5) continue;

            // Skip header/footer/summary lines
            var lower = line.ToLowerInvariant();
            if (SkipKeywords.Any(kw => lower.StartsWith(kw))) continue;

            var decimalMatches = GermanDecimalRegex().Matches(line);
            if (decimalMatches.Count == 0) continue;

            // Use second-to-last decimal as unit price (last is often the total)
            var priceMatch = decimalMatches.Count >= 2
                ? decimalMatches[decimalMatches.Count - 2]
                : decimalMatches[0];

            // Price must not appear at the very start (would mean it's not a position line)
            if (priceMatch.Index < 10) continue;

            var price = ParseGermanDecimal(priceMatch.Value);
            if (price <= 0) continue;

            // Build description from everything before the price match
            var beforePrice = line[..priceMatch.Index].TrimEnd();
            beforePrice = LeadingPositionRegex().Replace(beforePrice, "").TrimStart();

            // Try to extract quantity (integer-like number just before price)
            decimal menge = 1;
            var qtyMatch = Regex.Match(beforePrice, @"(\d+(?:,\d+)?)\s+\w{0,10}\s*$");
            if (qtyMatch.Success)
            {
                var qty = ParseGermanDecimal(qtyMatch.Groups[1].Value);
                if (qty > 0 && qty <= 100_000) menge = qty;
            }

            // Strip trailing qty + unit from description
            var description = TrailingQtyUnitRegex().Replace(beforePrice, "").Trim();
            if (description.Length < 3)
                description = beforePrice.Trim(); // fallback without stripping

            if (description.Length < 3) continue;

            posNr++;
            positionen.Add(new Angebotsposition
            {
                Id = Guid.NewGuid(),
                Position = posNr,
                Text = description,
                Menge = menge,
                Einheit = string.Empty,
                Einzelpreis = price,
                Steuersatz = 19m
            });
        }

        if (positionen.Count == 0)
        {
            warnungen.Add(new ImportWarnung
            {
                Meldung = "Keine Positionen erkannt. Bitte Positionen im Editor manuell erfassen."
            });

            positionen.Add(new Angebotsposition
            {
                Id = Guid.NewGuid(),
                Position = 1,
                Text = $"Importiert aus: {dateiName}",
                Menge = 1,
                Einheit = string.Empty,
                Einzelpreis = 0,
                Steuersatz = 19m
            });
        }
        else if (positionen.Count > 50)
        {
            warnungen.Add(new ImportWarnung
            {
                Meldung = $"{positionen.Count} Positionen erkannt – bitte im Editor auf Plausibilität prüfen."
            });
        }

        return (positionen, warnungen);
    }

    private static decimal ParseGermanDecimal(string value)
    {
        // 1.234,56 → remove thousand dots, swap decimal comma for dot
        var normalized = value.Replace(".", "").Replace(",", ".");
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }

    private async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string fileName, string arguments)
    {
        var timeout = _configuration.GetValue<int>("Ocr:TimeoutSeconds", 60);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Prozess '{fileName}' hat das Timeout von {timeout} Sekunden überschritten.");
        }

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
