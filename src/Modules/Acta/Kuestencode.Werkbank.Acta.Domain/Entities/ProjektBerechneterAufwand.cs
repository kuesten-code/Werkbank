namespace Kuestencode.Werkbank.Acta.Domain.Entities;

public class ProjektBerechneterAufwand
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Belegnummer { get; set; } = "";
    public string Lieferant { get; set; } = "";
    public decimal Netto { get; set; }
    public decimal Brutto { get; set; }
    public DateTime BerechnedAt { get; set; }

    public Project Project { get; set; } = null!;
}
