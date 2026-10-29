namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

/// <summary>
/// Dateianhang (Grundriss, Beleg, Abrechnungs-PDF). Liegt in der Datenbank und ist damit im Backup enthalten.
/// </summary>
public class Dokument
{
    public Guid Id { get; set; }
    public string Dateiname { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Groesse { get; set; }
    public byte[] Inhalt { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
}
