using System.Net.Http.Json;
using Kuestencode.Shared.Contracts.Recepta;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Ruft Belege direkt vom Recepta-Dienst ab (service-to-service HTTP).
/// Filtert nach PaidDate (Zufluss-/Abflussprinzip für EÜR).
/// </summary>
public class ReceptaDataService : IReceptaDataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReceptaDataService> _logger;

    public ReceptaDataService(HttpClient httpClient, ILogger<ReceptaDataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<ReceptaDocumentDto>> GetDocumentsAsync(DateOnly von, DateOnly bis)
    {
        try
        {
            // Zufluss-/Abflussprinzip: Filter nach PaidDate, nicht InvoiceDate
            var url = $"/api/recepta/documents?status=Paid&paidFrom={von:yyyy-MM-dd}&paidTo={bis:yyyy-MM-dd}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Recepta API returned {StatusCode} for documents request", response.StatusCode);
                return new List<ReceptaDocumentDto>();
            }

            var documents = await response.Content.ReadFromJsonAsync<List<ReceptaDocumentDto>>();
            return documents ?? new List<ReceptaDocumentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching documents from Recepta for period {Von} - {Bis}", von, bis);
            return new List<ReceptaDocumentDto>();
        }
    }
}
