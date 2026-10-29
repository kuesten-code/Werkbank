namespace Kuestencode.Werkbank.Locatio.Domain.Entities;

public class Gebaeude : Eigentumsobjekt
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Strasse { get; set; }
    public string? Plz { get; set; }
    public string? Ort { get; set; }

    /// <summary>Link zum externen Nebenkostenabrechner (z. B. Kalo).</summary>
    public string? AbrechnerUrl { get; set; }

    public string? Notizen { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<Mieteinheit> Einheiten { get; set; } = new();

    public string Adresse =>
        string.Join(", ", new[] { Strasse, $"{Plz} {Ort}".Trim() }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
