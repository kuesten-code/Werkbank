namespace Kuestencode.Shared.Contracts.Acta;

/// <summary>
/// Leichtgewichtiges Projekt-DTO für die Verwendung durch andere Module (z.B. Rapport).
/// Verwendet ExternalId als ID für Cross-Modul-Kompatibilität.
/// </summary>
public class ActaProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ProjectNumber { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
}
