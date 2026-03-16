namespace Kuestencode.Werkbank.Saldo.Services;

public interface IPdfReportService
{
    Task<byte[]> GenerateEuerReportAsync(DateOnly von, DateOnly bis);
}
