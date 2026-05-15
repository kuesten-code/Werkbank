namespace Kuestencode.Werkbank.Acta.Domain.Entities;

public class ProjektStundensatz
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public int RolleId { get; set; }
    public string RolleName { get; set; } = "";
    public decimal Stundensatz { get; set; }
    public DateTime ErstelltAm { get; set; }

    public Project Project { get; set; } = null!;
}
