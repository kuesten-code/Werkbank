using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Berechnet die EÜR (Einnahmen-Überschuss-Rechnung) aus Faktura- und Recepta-Daten.
/// </summary>
public class EuerService : IEuerService
{
    private readonly IReceptaDataService _receptaData;
    private readonly IFakturaApiClient _fakturaClient;
    private readonly IKategorieKontoMappingRepository _mappingRepo;
    private readonly IKontoRepository _kontoRepo;
    private readonly ISaldoSettingsRepository _settingsRepo;
    private readonly ILogger<EuerService> _logger;

    public EuerService(
        IReceptaDataService receptaData,
        IFakturaApiClient fakturaClient,
        IKategorieKontoMappingRepository mappingRepo,
        IKontoRepository kontoRepo,
        ISaldoSettingsRepository settingsRepo,
        ILogger<EuerService> logger)
    {
        _receptaData = receptaData;
        _fakturaClient = fakturaClient;
        _mappingRepo = mappingRepo;
        _kontoRepo = kontoRepo;
        _settingsRepo = settingsRepo;
        _logger = logger;
    }

    public async Task<EuerSummaryDto> GetEuerSummaryAsync(EuerFilterDto filter)
    {
        var settings = await _settingsRepo.GetAsync();
        var kontenrahmen = settings?.Kontenrahmen ?? "SKR03";

        // Lade Konten und Mappings
        var konten = await _kontoRepo.GetByKontenrahmenAsync(kontenrahmen);
        var mappings = await _mappingRepo.GetAllAsync(kontenrahmen);

        // Faktura-Einnahmen
        var fakturaFilter = new InvoiceFilterDto
        {
            FromDate = filter.Von.ToDateTime(TimeOnly.MinValue),
            ToDate = filter.Bis.ToDateTime(TimeOnly.MaxValue)
        };

        List<InvoiceDto> invoices = new();
        try
        {
            invoices = await _fakturaClient.GetAllInvoicesAsync(fakturaFilter);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Faktura-Daten für EÜR");
        }

        // Recepta-Ausgaben
        var receptaDocs = await _receptaData.GetDocumentsAsync(filter.Von, filter.Bis);

        // Einnahmen aggregieren (nach USt-Satz → Konto)
        var einnahmenPositionen = AggregateEinnahmen(invoices, konten, filter);

        // Ausgaben aggregieren (nach Kategorie → Konto)
        var ausgabenPositionen = AggregateAusgaben(receptaDocs, mappings, konten, filter);

        var summary = new EuerSummaryDto
        {
            Von = filter.Von,
            Bis = filter.Bis,
            Einnahmen = einnahmenPositionen,
            Ausgaben = ausgabenPositionen,
            EinnahmenNetto = einnahmenPositionen.Sum(p => p.BetragNetto),
            EinnahmenMwst = einnahmenPositionen.Sum(p => p.MwstBetrag),
            EinnahmenBrutto = einnahmenPositionen.Sum(p => p.BetragBrutto),
            AusgabenNetto = ausgabenPositionen.Sum(p => p.BetragNetto),
            AusgabenMwst = ausgabenPositionen.Sum(p => p.MwstBetrag),
            AusgabenBrutto = ausgabenPositionen.Sum(p => p.BetragBrutto)
        };

        return summary;
    }

    private List<EuerPositionDto> AggregateEinnahmen(
        List<InvoiceDto> invoices,
        List<Konto> konten,
        EuerFilterDto filter)
    {
        // Filtere nur bezahlte/versendete Rechnungen
        var relevantInvoices = invoices
            .Where(i => i.Status is "Sent" or "Paid")
            .Where(i => DateOnly.FromDateTime(i.InvoiceDate) >= filter.Von &&
                        DateOnly.FromDateTime(i.InvoiceDate) <= filter.Bis)
            .ToList();

        // Gruppiere nach effektivem USt-Satz (aus Items)
        var groups = relevantInvoices
            .SelectMany(inv => inv.Items.Select(item => new
            {
                VatRate = item.VatRate,
                NetBetrag = item.TotalNet,
                VatBetrag = item.TotalVat,
                GrossBetrag = item.TotalGross
            }))
            .GroupBy(x => x.VatRate)
            .ToList();

        var positionen = new List<EuerPositionDto>();
        foreach (var group in groups)
        {
            var konto = FindEinnahmeKonto(konten, group.Key);
            positionen.Add(new EuerPositionDto
            {
                KontoNummer = konto?.KontoNummer ?? "8400",
                KontoBezeichnung = konto?.KontoBezeichnung ?? $"Erlöse {group.Key}% USt",
                Gruppe = "Erlöse",
                BetragNetto = group.Sum(x => x.NetBetrag),
                MwstBetrag = group.Sum(x => x.VatBetrag),
                BetragBrutto = group.Sum(x => x.GrossBetrag),
                AnzahlBelege = relevantInvoices.Count(inv => inv.Items.Any(i => i.VatRate == group.Key))
            });
        }

        return positionen;
    }

    private Konto? FindEinnahmeKonto(List<Konto> konten, decimal vatRate) =>
        konten.FirstOrDefault(k =>
            k.KontoTyp == KontoTyp.Einnahme &&
            k.UstSatz.HasValue &&
            k.UstSatz.Value == vatRate);

    private List<EuerPositionDto> AggregateAusgaben(
        List<Kuestencode.Shared.Contracts.Recepta.ReceptaDocumentDto> docs,
        List<KategorieKontoMapping> mappings,
        List<Konto> konten,
        EuerFilterDto filter)
    {
        // Filtere verbuchte/bezahlte Belege im Zeitraum
        var relevantDocs = docs
            .Where(d => d.Status is "Booked" or "Paid")
            .ToList();

        var positionen = new List<EuerPositionDto>();

        var groups = relevantDocs.GroupBy(d => d.Category).ToList();
        foreach (var group in groups)
        {
            var mapping = mappings.FirstOrDefault(m => m.ReceiptaKategorie == group.Key);
            var konto = mapping != null
                ? konten.FirstOrDefault(k => k.KontoNummer == mapping.KontoNummer)
                : null;

            var amountNet = group.Sum(d => d.AmountNet);
            var amountGross = group.Sum(d => d.AmountGross);

            positionen.Add(new EuerPositionDto
            {
                KontoNummer = konto?.KontoNummer ?? mapping?.KontoNummer ?? "4980",
                KontoBezeichnung = konto?.KontoBezeichnung ?? group.Key,
                Gruppe = konto != null ? GetGruppe(konto) : "Sonstiges",
                BetragNetto = amountNet,
                MwstBetrag = amountGross - amountNet,
                BetragBrutto = amountGross,
                AnzahlBelege = group.Count()
            });
        }

        return positionen.OrderBy(p => p.KontoNummer).ToList();
    }

    private static string GetGruppe(Konto konto) => konto.KontoTyp switch
    {
        KontoTyp.Ausgabe => "Betriebsausgaben",
        KontoTyp.Bank => "Bank",
        _ => "Sonstiges"
    };
}
