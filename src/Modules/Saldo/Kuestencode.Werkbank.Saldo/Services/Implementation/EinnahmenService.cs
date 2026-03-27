using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Aggregiert bezahlte Faktura-Rechnungen nach Zufluss-/Abflussprinzip (PaidDate).
/// </summary>
public class EinnahmenService : IEinnahmenService
{
    private readonly IFakturaApiClient _fakturaClient;
    private readonly IKontoRepository _kontoRepo;
    private readonly ISaldoSettingsRepository _settingsRepo;
    private readonly ILogger<EinnahmenService> _logger;

    public EinnahmenService(
        IFakturaApiClient fakturaClient,
        IKontoRepository kontoRepo,
        ISaldoSettingsRepository settingsRepo,
        ILogger<EinnahmenService> logger)
    {
        _fakturaClient = fakturaClient;
        _kontoRepo = kontoRepo;
        _settingsRepo = settingsRepo;
        _logger = logger;
    }

    public async Task<List<BuchungDto>> GetEinnahmenAsync(DateOnly von, DateOnly bis)
    {
        var invoices = await LoadBezahlteRechnungenAsync(von, bis);
        var settings = await _settingsRepo.GetAsync();
        var kontenrahmen = settings?.Kontenrahmen ?? "SKR03";
        var konten = await _kontoRepo.GetByKontenrahmenAsync(kontenrahmen);

        var buchungen = new List<BuchungDto>();
        foreach (var inv in invoices)
        {
            var paidDate = inv.PaidDate.HasValue
                ? DateOnly.FromDateTime(inv.PaidDate.Value)
                : DateOnly.FromDateTime(inv.InvoiceDate);

            foreach (var item in inv.Items)
            {
                var konto = konten.FirstOrDefault(k =>
                    k.KontoTyp == KontoTyp.Einnahme &&
                    k.UstSatz.HasValue &&
                    k.UstSatz.Value == item.VatRate);

                buchungen.Add(new BuchungDto
                {
                    Id = Guid.NewGuid(),
                    Quelle = "Faktura",
                    QuelleId = inv.InvoiceNumber,
                    BelegDatum = DateOnly.FromDateTime(inv.InvoiceDate),
                    ZahlungsDatum = paidDate,
                    Beschreibung = inv.CustomerName ?? $"Kunde #{inv.CustomerId}",
                    Netto = item.TotalNet,
                    Ust = item.TotalVat,
                    Brutto = item.TotalGross,
                    UstSatz = item.VatRate,
                    KontoNummer = konto?.KontoNummer ?? "8400",
                    KontoBezeichnung = konto?.KontoBezeichnung ?? $"Erlöse {item.VatRate}% USt",
                    Typ = BuchungsTyp.Einnahme
                });
            }
        }

        return buchungen.OrderBy(b => b.ZahlungsDatum).ToList();
    }

    public async Task<decimal> GetSummeAsync(DateOnly von, DateOnly bis)
    {
        var invoices = await LoadBezahlteRechnungenAsync(von, bis);
        return invoices.Sum(i => i.TotalNetAfterDiscount);
    }

    public async Task<Dictionary<string, decimal>> GetNachUstSatzAsync(DateOnly von, DateOnly bis)
    {
        var invoices = await LoadBezahlteRechnungenAsync(von, bis);
        return invoices
            .SelectMany(i => i.Items)
            .GroupBy(item => $"{item.VatRate}%")
            .ToDictionary(g => g.Key, g => g.Sum(item => item.TotalNet));
    }

    private async Task<List<InvoiceDto>> LoadBezahlteRechnungenAsync(DateOnly von, DateOnly bis)
    {
        try
        {
            var filter = new InvoiceFilterDto
            {
                Status = "Paid",
                PaidFrom = von.ToDateTime(TimeOnly.MinValue),
                PaidTo = bis.ToDateTime(TimeOnly.MaxValue)
            };
            return await _fakturaClient.GetAllInvoicesAsync(filter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden bezahlter Faktura-Rechnungen für {Von} - {Bis}", von, bis);
            return new List<InvoiceDto>();
        }
    }
}
