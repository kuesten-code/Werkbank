namespace Kuestencode.Werkbank.Contracta.Services.Interfaces;

public interface IRechnungserstellungService
{
    bool IstVerfuegbar { get; }
    Task<RechnungserstellungErgebnis> ErstelleRechnungAsync(Guid vertragId);
    Task<List<RechnungserstellungErgebnis>> ErstelleRechnungenAsync(List<Guid> vertragIds);
}

public class RechnungserstellungErgebnis
{
    public Guid VertragId { get; set; }
    public bool Erfolgreich { get; set; }
    public int? RechnungId { get; set; }
    public string? Rechnungsnummer { get; set; }
    public string? Fehler { get; set; }
}
