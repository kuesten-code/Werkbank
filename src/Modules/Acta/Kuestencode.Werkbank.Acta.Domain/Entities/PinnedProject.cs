namespace Kuestencode.Werkbank.Acta.Domain.Entities;

/// <summary>
/// Pro-Nutzer-Anheftung eines Projekts als Dashboard-Kachel.
/// </summary>
public class PinnedProject
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime PinnedAt { get; set; }

    public Project Project { get; set; } = null!;
}
