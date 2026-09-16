using Kuestencode.Core.Interfaces;
using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Berechnet die EÜR (Einnahmen-Überschuss-Rechnung) aus den Einnahmen-/Ausgaben-Buchungen.
/// Nutzt dieselbe Zahlungsebene wie SaldoAggregationService: jede Teilzahlung (Anzahlung, Rate,
/// Skonto) wird zu ihrem tatsächlichen Zahlungsdatum und anteilig verbucht, statt die gesamte
/// Rechnung/den gesamten Beleg einmalig zum Datum der Schlusszahlung zu buchen.
/// </summary>
public class EuerService : IEuerService
{
    private readonly IEinnahmenService _einnahmen;
    private readonly IAusgabenService _ausgaben;
    private readonly IKontoRepository _kontoRepo;
    private readonly ISaldoSettingsRepository _settingsRepo;
    private readonly ICompanyService _companyService;

    public EuerService(
        IEinnahmenService einnahmen,
        IAusgabenService ausgaben,
        IKontoRepository kontoRepo,
        ISaldoSettingsRepository settingsRepo,
        ICompanyService companyService)
    {
        _einnahmen = einnahmen;
        _ausgaben = ausgaben;
        _kontoRepo = kontoRepo;
        _settingsRepo = settingsRepo;
        _companyService = companyService;
    }

    public async Task<EuerSummaryDto> GetEuerSummaryAsync(EuerFilterDto filter)
    {
        var settings = await _settingsRepo.GetAsync();
        var kontenrahmen = settings?.Kontenrahmen ?? "SKR03";
        bool istKleinunternehmer = false;
        try { var company = await _companyService.GetCompanyAsync(); istKleinunternehmer = company?.IsKleinunternehmer ?? false; } catch { }

        var konten = await _kontoRepo.GetByKontenrahmenAsync(kontenrahmen);

        var einnahmenBuchungen = await _einnahmen.GetEinnahmenAsync(filter.Von, filter.Bis);
        var ausgabenBuchungen = await _ausgaben.GetAusgabenAsync(filter.Von, filter.Bis);

        var einnahmenPositionen = AggregateByKonto(einnahmenBuchungen, konten);
        var ausgabenPositionen = AggregateByKonto(ausgabenBuchungen, konten);

        return new EuerSummaryDto
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
            AusgabenBrutto = ausgabenPositionen.Sum(p => p.BetragBrutto),
            IstKleinunternehmer = istKleinunternehmer
        };
    }

    private static List<EuerPositionDto> AggregateByKonto(List<BuchungDto> buchungen, List<Konto> konten)
    {
        return buchungen
            .GroupBy(b => b.KontoNummer)
            .Select(group =>
            {
                var konto = konten.FirstOrDefault(k => k.KontoNummer == group.Key);
                return new EuerPositionDto
                {
                    KontoNummer = group.Key,
                    KontoBezeichnung = konto?.KontoBezeichnung ?? group.First().KontoBezeichnung,
                    Gruppe = GetGruppe(konto, group.First().Typ),
                    BetragNetto = group.Sum(b => b.Netto),
                    MwstBetrag = group.Sum(b => b.Ust),
                    BetragBrutto = group.Sum(b => b.Brutto),
                    AnzahlBelege = group.Count()
                };
            })
            .OrderBy(p => p.KontoNummer)
            .ToList();
    }

    private static string GetGruppe(Konto? konto, BuchungsTyp typ)
    {
        if (konto == null)
            return typ == BuchungsTyp.Einnahme ? "Erlöse" : "Sonstiges";

        return konto.KontoTyp switch
        {
            KontoTyp.Einnahme => "Erlöse",
            KontoTyp.Ausgabe => "Betriebsausgaben",
            KontoTyp.Bank => "Bank",
            _ => "Sonstiges"
        };
    }
}
