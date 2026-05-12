using Kuestencode.Shared.ApiClients;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Aggregiert Faktura-Zahlungen nach Zufluss-/Abflussprinzip (§11 EStG, PaymentDate).
/// Teilzahlungen erscheinen im Monat ihrer Zahlung; Positionen werden anteilig aufgeteilt.
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
        var payments = await LoadZahlungenAsync(von, bis);
        var settings = await _settingsRepo.GetAsync();
        var kontenrahmen = settings?.Kontenrahmen ?? "SKR03";
        var konten = await _kontoRepo.GetByKontenrahmenAsync(kontenrahmen);

        var buchungen = new List<BuchungDto>();

        // Group by InvoiceId to detect partial payments (/1, /2 suffix)
        var byInvoice = payments.GroupBy(p => p.InvoiceId).ToList();

        foreach (var group in byInvoice)
        {
            var isPartial = group.Count() > 1;
            var invoicePayments = group.OrderBy(p => p.PaymentDate).ToList();

            for (var n = 0; n < invoicePayments.Count; n++)
            {
                var payment = invoicePayments[n];
                var ratio = payment.InvoiceTotalGross > 0
                    ? payment.PaymentAmount / payment.InvoiceTotalGross
                    : 1m;
                var quelleId = isPartial
                    ? $"{payment.InvoiceNumber}/{n + 1}"
                    : payment.InvoiceNumber;

                foreach (var item in payment.Items)
                {
                    var konto = konten.FirstOrDefault(k =>
                        k.KontoTyp == KontoTyp.Einnahme &&
                        k.UstSatz.HasValue &&
                        k.UstSatz.Value == item.VatRate);

                    buchungen.Add(new BuchungDto
                    {
                        Id = Guid.NewGuid(),
                        Quelle = "Faktura",
                        QuelleId = quelleId,
                        BelegDatum = DateOnly.FromDateTime(payment.InvoiceDate),
                        ZahlungsDatum = payment.PaymentDate,
                        Beschreibung = payment.CustomerName ?? $"Rechnung {payment.InvoiceNumber}",
                        Netto = item.TotalNet * ratio,
                        Ust = item.TotalVat * ratio,
                        Brutto = item.TotalGross * ratio,
                        UstSatz = item.VatRate,
                        KontoNummer = konto?.KontoNummer ?? "8400",
                        KontoBezeichnung = konto?.KontoBezeichnung ?? $"Erlöse {item.VatRate}% USt",
                        Typ = BuchungsTyp.Einnahme
                    });
                }
            }
        }

        return buchungen.OrderBy(b => b.ZahlungsDatum).ToList();
    }

    public async Task<decimal> GetSummeAsync(DateOnly von, DateOnly bis)
    {
        var payments = await LoadZahlungenAsync(von, bis);
        return payments.Sum(p =>
        {
            var ratio = p.InvoiceTotalGross > 0 ? p.PaymentAmount / p.InvoiceTotalGross : 1m;
            return p.Items.Sum(i => i.TotalNet) * ratio;
        });
    }

    public async Task<Dictionary<string, decimal>> GetNachUstSatzAsync(DateOnly von, DateOnly bis)
    {
        var payments = await LoadZahlungenAsync(von, bis);
        return payments
            .SelectMany(p =>
            {
                var ratio = p.InvoiceTotalGross > 0 ? p.PaymentAmount / p.InvoiceTotalGross : 1m;
                return p.Items.Select(i => (Key: $"{i.VatRate}%", Net: i.TotalNet * ratio));
            })
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Net));
    }

    private async Task<List<Kuestencode.Shared.Contracts.Faktura.InvoiceEuerPaymentDto>> LoadZahlungenAsync(DateOnly von, DateOnly bis)
    {
        try
        {
            return await _fakturaClient.GetEuerPaymentsAsync(von, bis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Faktura-EÜR-Zahlungen für {Von} - {Bis}", von, bis);
            return [];
        }
    }
}
