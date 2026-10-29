namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

public class Darlehen
{
    public Guid Id { get; set; }
    public Guid GebaeudeId { get; set; }
    public Gebaeude? Gebaeude { get; set; }

    public string Bezeichnung { get; set; } = string.Empty;
    public string? Kreditgeber { get; set; }
    public string? Notiz { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Darlehenszahlung> Zahlungen { get; set; } = new();
}
