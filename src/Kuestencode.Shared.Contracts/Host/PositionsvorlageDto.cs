namespace Kuestencode.Shared.Contracts.Host;

public class PositionsvorlageDto
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
    public string? Einheit { get; set; }
    public decimal Einzelpreis { get; set; }
}

public class PositionsvorlageMerkenDto
{
    public string Text { get; set; } = "";
    public string? Einheit { get; set; }
    public decimal Einzelpreis { get; set; }
    public DateTime Dokumentdatum { get; set; }
}
