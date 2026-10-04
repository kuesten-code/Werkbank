using System.Text;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

/// <summary>
/// Prüft Uploads gegen die erlaubten Typen (png, jpg, webp, pdf) und die Größenlimits.
/// Der Typ wird ausschließlich aus den Signaturbytes abgeleitet — weder der vom Browser
/// gemeldete Content-Type noch die Dateiendung sind vertrauenswürdig.
/// </summary>
public static class FeedbackUploadPolicy
{
    public const int DefaultMaxFileSizeMb = 5;
    public const int DefaultMaxReportSizeMb = 20;
    public const int MaxFiles = 10;

    public static (List<string> Errors, List<ValidatedFeedbackUpload> Files) Validate(
        IReadOnlyList<FeedbackUpload> uploads, int maxFileSizeMb, int maxReportSizeMb)
    {
        var errors = new List<string>();
        var files = new List<ValidatedFeedbackUpload>();
        var maxFileBytes = maxFileSizeMb * 1024L * 1024L;
        var maxTotalBytes = maxReportSizeMb * 1024L * 1024L;

        if (uploads.Count > MaxFiles)
            errors.Add($"Höchstens {MaxFiles} Dateien pro Meldung erlaubt.");

        foreach (var upload in uploads)
        {
            var fileName = SanitizeFileName(upload.FileName);

            if (upload.Content.Length == 0)
            {
                errors.Add($"Datei '{fileName}' ist leer.");
                continue;
            }

            if (upload.Content.Length > maxFileBytes)
            {
                errors.Add($"Datei '{fileName}' ist größer als {maxFileSizeMb} MB.");
                continue;
            }

            var detected = DetectType(upload.Content);
            if (detected == null)
            {
                errors.Add($"Datei '{fileName}' hat keinen erlaubten Typ (png, jpg, webp, pdf).");
                continue;
            }

            files.Add(new ValidatedFeedbackUpload(fileName, detected.Value.ContentType, detected.Value.Extension, upload.Content));
        }

        if (uploads.Sum(u => (long)u.Content.Length) > maxTotalBytes)
            errors.Add($"Alle Dateien zusammen dürfen höchstens {maxReportSizeMb} MB groß sein.");

        return (errors, files);
    }

    public static (string ContentType, string Extension)? DetectType(ReadOnlySpan<byte> content)
    {
        if (content.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            return ("image/png", ".png");
        if (content.StartsWith(new byte[] { 0xFF, 0xD8, 0xFF }))
            return ("image/jpeg", ".jpg");
        if (content.Length >= 12 && content[..4].SequenceEqual("RIFF"u8) && content.Slice(8, 4).SequenceEqual("WEBP"u8))
            return ("image/webp", ".webp");
        if (content.StartsWith("%PDF-"u8))
            return ("application/pdf", ".pdf");
        return null;
    }

    /// <summary>Nur zur Anzeige — als Speicherpfad wird der Name nie verwendet.</summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = Path.GetFileName((fileName ?? "").Replace('\\', '/'));
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
            builder.Append(invalid.Contains(c) || char.IsControl(c) ? '_' : c);

        var sanitized = builder.ToString().Trim().TrimStart('.');
        if (sanitized.Length == 0)
            sanitized = "datei";
        return sanitized.Length > 200 ? sanitized[..200] : sanitized;
    }
}
