namespace Kuestencode.Shared.Contracts.Recepta;

/// <summary>
/// Zusammenfassung der externen Kosten (Recepta-Belege) für ein Projekt.
/// Analog zu ProjectHoursResponseDto aus Rapport.
/// </summary>
public class ProjectExpensesResponseDto
{
    public Guid ProjectId { get; set; }
    public decimal TotalNet { get; set; }
    public decimal TotalGross { get; set; }
    public int DocumentCount { get; set; }
    public List<ReceptaDocumentDto> Documents { get; set; } = new();
}
