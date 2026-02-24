namespace Kuestencode.Shared.Contracts.Recepta;

/// <summary>
/// Leichtgewichtiges Datei-DTO für Recepta-Beleganhänge.
/// </summary>
public class ReceptaDocumentFileDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
