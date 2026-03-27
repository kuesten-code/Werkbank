using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Aggregiert bezahlte Recepta-Belege nach Zufluss-/Abflussprinzip (PaidDate).
/// </summary>
public class AusgabenService : IAusgabenService
{
    private readonly IReceptaDataService _receptaData;
    private readonly IKategorieKontoMappingRepository _mappingRepo;
    private readonly IKontoRepository _kontoRepo;
    private readonly ISaldoSettingsRepository _settingsRepo;
    private readonly ILogger<AusgabenService> _logger;

    public AusgabenService(
        IReceptaDataService receptaData,
        IKategorieKontoMappingRepository mappingRepo,
        IKontoRepository kontoRepo,
        ISaldoSettingsRepository settingsRepo,
        ILogger<AusgabenService> logger)
    {
        _receptaData = receptaData;
        _mappingRepo = mappingRepo;
        _kontoRepo = kontoRepo;
        _settingsRepo = settingsRepo;
        _logger = logger;
    }

    public async Task<List<BuchungDto>> GetAusgabenAsync(DateOnly von, DateOnly bis)
    {
        // ReceptaDataService filtert bereits nach status=Paid&paidFrom=...&paidTo=...
        var docs = await _receptaData.GetDocumentsAsync(von, bis);
        var settings = await _settingsRepo.GetAsync();
        var kontenrahmen = settings?.Kontenrahmen ?? "SKR03";
        var mappings = await _mappingRepo.GetAllAsync(kontenrahmen);
        var konten = await _kontoRepo.GetByKontenrahmenAsync(kontenrahmen);

        var fallbackKonto = kontenrahmen == "SKR04" ? "4900" : "4980";

        var buchungen = new List<BuchungDto>();
        foreach (var doc in docs)
        {
            var paidDate = doc.PaidDate ?? doc.InvoiceDate;
            var mapping = mappings.FirstOrDefault(m => m.ReceiptaKategorie == doc.Category);
            var konto = mapping != null
                ? konten.FirstOrDefault(k => k.KontoNummer == mapping.KontoNummer)
                : null;

            buchungen.Add(new BuchungDto
            {
                Id = doc.Id,
                Quelle = "Recepta",
                QuelleId = doc.DocumentNumber,
                BelegDatum = doc.InvoiceDate,
                ZahlungsDatum = paidDate,
                Beschreibung = doc.SupplierName,
                Netto = doc.AmountNet,
                Ust = doc.AmountTax,
                Brutto = doc.AmountGross,
                UstSatz = doc.TaxRate,
                Kategorie = doc.Category,
                KontoNummer = konto?.KontoNummer ?? mapping?.KontoNummer ?? fallbackKonto,
                KontoBezeichnung = konto?.KontoBezeichnung ?? doc.Category,
                Typ = BuchungsTyp.Ausgabe
            });
        }

        return buchungen.OrderBy(b => b.ZahlungsDatum).ToList();
    }

    public async Task<decimal> GetSummeAsync(DateOnly von, DateOnly bis)
    {
        var docs = await _receptaData.GetDocumentsAsync(von, bis);
        return docs.Sum(d => d.AmountNet);
    }

    public async Task<Dictionary<string, decimal>> GetNachKategorieAsync(DateOnly von, DateOnly bis)
    {
        var docs = await _receptaData.GetDocumentsAsync(von, bis);
        return docs
            .GroupBy(d => d.Category)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.AmountNet));
    }
}
