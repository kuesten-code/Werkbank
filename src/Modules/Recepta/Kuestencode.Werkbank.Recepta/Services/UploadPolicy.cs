namespace Kuestencode.Werkbank.Recepta.Services;

/// <summary>
/// Regeln für Beleg-Anhänge an der Systemgrenze (API und Blazor-Upload): welche Typen und Größen angenommen
/// und welche davon im Browser angezeigt werden dürfen.
/// </summary>
public static class UploadPolicy
{
    public const long MaxFileSize = 50L * 1024 * 1024;
    public const string FallbackFileName = "datei";

    private static readonly Dictionary<string, string> CanonicalContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = "application/pdf",
        ["image/jpeg"] = "image/jpeg",
        ["image/jpg"] = "image/jpeg",
        ["image/png"] = "image/png",
        ["image/tiff"] = "image/tiff",
        ["text/xml"] = "application/xml",
        ["application/xml"] = "application/xml"
    };

    /// <summary>
    /// XML bleibt außen vor: ein im Browser geöffnetes XML-Dokument kann über XHTML-Namespaces Skripte ausführen.
    /// </summary>
    private static readonly HashSet<string> InlineContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", "image/tiff"
    };

    public static string? NormalizeContentType(string? contentType) =>
        contentType != null && CanonicalContentTypes.TryGetValue(contentType.Trim(), out var canonical) ? canonical : null;

    public static bool MayDisplayInline(string contentType) => InlineContentTypes.Contains(contentType);

    /// <summary>
    /// Nur der reine Dateiname ohne Pfadanteile, egal ob mit / oder \ getrennt.
    /// </summary>
    public static string SanitizeFileName(string? fileName)
    {
        var name = (fileName ?? "").Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];
        name = new string(name.Where(c => !char.IsControl(c) && !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
        return name is "" or "." or ".." ? FallbackFileName : name;
    }
}
