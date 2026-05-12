using System.ComponentModel.DataAnnotations.Schema;

namespace Kuestencode.Werkbank.Recepta.Domain.Entities;

public class DocumentPayment
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public DateOnly PaymentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
