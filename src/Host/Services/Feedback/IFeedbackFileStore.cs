namespace Kuestencode.Werkbank.Host.Services.Feedback;

public interface IFeedbackFileStore
{
    /// <summary>Speichert die Datei unter einem serverseitig erzeugten Namen und liefert den relativen Pfad.</summary>
    Task<string> SaveAsync(string folder, string extension, byte[] content, CancellationToken ct = default);

    Stream OpenRead(string relativePath);
    Task<byte[]> ReadAllBytesAsync(string relativePath, CancellationToken ct = default);
    void Delete(string relativePath);
}
