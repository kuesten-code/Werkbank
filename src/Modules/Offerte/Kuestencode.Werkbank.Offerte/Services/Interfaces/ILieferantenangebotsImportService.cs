namespace Kuestencode.Werkbank.Offerte.Services;

public class AngebotImportErgebnis
{
    public bool Erfolgreich { get; set; }
    public Guid? AngebotId { get; set; }
    public string? Angebotsnummer { get; set; }
    public List<ImportWarnung> Warnungen { get; set; } = [];
    public string? Fehler { get; set; }
}

public class ImportWarnung
{
    public int? Position { get; set; }
    public string Meldung { get; set; } = string.Empty;
}

public interface ILieferantenangebotsImportService
{
    Task<AngebotImportErgebnis> ImportiereAsync(
        Stream pdfStream,
        string dateiName,
        decimal aufschlagProzent,
        int kundeId);
}
