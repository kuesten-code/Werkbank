using System.ComponentModel.DataAnnotations;

namespace Kuestencode.Faktura.Models;

public class InvoiceAttachment
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long FileSize { get; set; }

    [Required]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Invoice Invoice { get; set; } = null!;
}
