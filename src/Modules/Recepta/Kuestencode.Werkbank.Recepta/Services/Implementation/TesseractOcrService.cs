using System.Diagnostics;
using System.Text;

namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// OCR-Service basierend auf Tesseract CLI und pdftoppm (poppler-utils).
/// Ruft die Binaries als externe Prozesse auf – robust in Docker-Containern.
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _language;
    private readonly int _pdfDpi;
    private readonly int _timeoutSeconds;

    private static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".bmp", ".webp"
    };

    private static readonly HashSet<string> SupportedPdfExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf"
    };

    public TesseractOcrService(ILogger<TesseractOcrService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _language = configuration.GetValue<string>("Ocr:Language") ?? "deu";
        _pdfDpi = configuration.GetValue<int>("Ocr:PdfDpi", 300);
        _timeoutSeconds = configuration.GetValue<int>("Ocr:TimeoutSeconds", 60);
    }

    public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
    {
        var extension = Path.GetExtension(fileName);

        if (string.IsNullOrEmpty(extension))
        {
            _logger.LogWarning("Datei '{FileName}' hat keine Dateiendung – OCR übersprungen", fileName);
            return string.Empty;
        }

        if (!SupportedImageExtensions.Contains(extension) && !SupportedPdfExtensions.Contains(extension))
        {
            _logger.LogWarning("Dateityp '{Extension}' wird für OCR nicht unterstützt", extension);
            return string.Empty;
        }

        // Temp-Datei für den Input erstellen
        var tempDir = Path.Combine(Path.GetTempPath(), $"ocr_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, $"input{extension}");
            await using (var fs = File.Create(inputPath))
            {
                await fileStream.CopyToAsync(fs);
            }

            string imagePath;

            if (SupportedPdfExtensions.Contains(extension))
            {
                // PDF → Bild konvertieren (erste Seite)
                imagePath = await ConvertPdfFirstPageToImageAsync(inputPath, tempDir);
            }
            else
            {
                imagePath = inputPath;
            }

            // Tesseract OCR auf dem Bild ausführen
            var text = await RunTesseractAsync(imagePath);
            _logger.LogInformation("OCR für '{FileName}' abgeschlossen: {CharCount} Zeichen erkannt",
                fileName, text.Length);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR-Fehler bei Datei '{FileName}'", fileName);
            return string.Empty;
        }
        finally
        {
            // Temp-Verzeichnis aufräumen
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Temp-Verzeichnis '{TempDir}' konnte nicht gelöscht werden", tempDir);
            }
        }
    }

    /// <summary>
    /// Konvertiert die erste Seite eines PDFs in ein PNG-Bild via pdftoppm.
    /// </summary>
    private async Task<string> ConvertPdfFirstPageToImageAsync(string pdfPath, string tempDir)
    {
        var outputPrefix = Path.Combine(tempDir, "page");

        var (exitCode, _, stderr) = await RunProcessAsync(
            "pdftoppm",
            $"-png -r {_pdfDpi} -f 1 -l 1 \"{pdfPath}\" \"{outputPrefix}\"");

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"pdftoppm-Fehler (Exit-Code {exitCode}): {stderr}");
        }

        // pdftoppm erzeugt Dateien wie page-1.png oder page-01.png
        var candidates = new[] { $"{outputPrefix}-1.png", $"{outputPrefix}-01.png" };
        var outputPath = candidates.FirstOrDefault(File.Exists);

        if (outputPath == null)
        {
            // Fallback: Suche nach beliebiger erzeugter PNG-Datei
            var pngFiles = Directory.GetFiles(tempDir, "page-*.png");
            outputPath = pngFiles.FirstOrDefault();
        }

        if (outputPath == null)
        {
            throw new InvalidOperationException(
                "pdftoppm hat keine Ausgabedatei erzeugt. Möglicherweise ist das PDF leer oder beschädigt.");
        }

        return outputPath;
    }

    /// <summary>
    /// Führt Tesseract OCR auf einer Bilddatei aus.
    /// </summary>
    private async Task<string> RunTesseractAsync(string imagePath)
    {
        // "stdout" als Output-Base sendet das Ergebnis direkt auf stdout
        var (exitCode, stdout, stderr) = await RunProcessAsync(
            "tesseract",
            $"\"{imagePath}\" stdout -l {_language}");

        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Tesseract-Fehler (Exit-Code {exitCode}): {stderr}");
        }

        return stdout.Trim();
    }

    /// <summary>
    /// Führt einen externen Prozess aus und gibt Exit-Code, stdout und stderr zurück.
    /// </summary>
    private async Task<(int ExitCode, string Stdout, string Stderr)> RunProcessAsync(
        string fileName, string arguments)
    {
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

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) stdout.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) stderr.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException(
                $"Prozess '{fileName}' hat das Timeout von {_timeoutSeconds} Sekunden überschritten.");
        }

        return (process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
