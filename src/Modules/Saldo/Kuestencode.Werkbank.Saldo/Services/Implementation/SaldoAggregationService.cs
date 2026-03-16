using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Kombiniert Einnahmen- und Ausgabendaten zu einer vollständigen Saldoübersicht.
/// </summary>
public class SaldoAggregationService : ISaldoAggregationService
{
    private readonly IEinnahmenService _einnahmen;
    private readonly IAusgabenService _ausgaben;

    public SaldoAggregationService(IEinnahmenService einnahmen, IAusgabenService ausgaben)
    {
        _einnahmen = einnahmen;
        _ausgaben = ausgaben;
    }

    public async Task<SaldoUebersichtDto> GetUebersichtAsync(DateOnly von, DateOnly bis)
    {
        var (einnahmenListe, ausgabenListe) = await LoadBeidesAsync(von, bis);

        var einnahmenNetto = einnahmenListe.Sum(b => b.Netto);
        var ausgabenNetto = ausgabenListe.Sum(b => b.Netto);
        var umsatzsteuer = einnahmenListe.Sum(b => b.Ust);
        var vorsteuer = ausgabenListe.Sum(b => b.Ust);

        return new SaldoUebersichtDto
        {
            Von = von,
            Bis = bis,
            Einnahmen = einnahmenNetto,
            Ausgaben = ausgabenNetto,
            Umsatzsteuer = umsatzsteuer,
            Vorsteuer = vorsteuer
        };
    }

    public async Task<List<BuchungDto>> GetAlleBuchungenAsync(DateOnly von, DateOnly bis)
    {
        var (einnahmen, ausgaben) = await LoadBeidesAsync(von, bis);
        return einnahmen.Concat(ausgaben).OrderBy(b => b.ZahlungsDatum).ToList();
    }

    public async Task<UstUebersichtDto> GetUstUebersichtAsync(DateOnly von, DateOnly bis)
    {
        var (einnahmen, ausgaben) = await LoadBeidesAsync(von, bis);

        var monate = einnahmen
            .GroupBy(b => new { b.ZahlungsDatum.Year, b.ZahlungsDatum.Month })
            .Select(g => new UstMonatDto
            {
                Jahr = g.Key.Year,
                Monat = g.Key.Month,
                Umsatzsteuer19 = g.Where(b => b.UstSatz == 19).Sum(b => b.Ust),
                Umsatzsteuer7 = g.Where(b => b.UstSatz == 7).Sum(b => b.Ust),
                VorsteuerGesamt = ausgaben
                    .Where(a => a.ZahlungsDatum.Year == g.Key.Year && a.ZahlungsDatum.Month == g.Key.Month)
                    .Sum(a => a.Ust)
            })
            .OrderBy(m => m.Jahr).ThenBy(m => m.Monat)
            .ToList();

        // Monate mit nur Ausgaben ergänzen
        var ausgabenMonate = ausgaben
            .GroupBy(b => new { b.ZahlungsDatum.Year, b.ZahlungsDatum.Month })
            .Where(g => !monate.Any(m => m.Jahr == g.Key.Year && m.Monat == g.Key.Month))
            .Select(g => new UstMonatDto
            {
                Jahr = g.Key.Year,
                Monat = g.Key.Month,
                VorsteuerGesamt = g.Sum(b => b.Ust)
            });

        monate.AddRange(ausgabenMonate);

        return new UstUebersichtDto
        {
            Monate = monate.OrderBy(m => m.Jahr).ThenBy(m => m.Monat).ToList()
        };
    }

    private async Task<(List<BuchungDto> Einnahmen, List<BuchungDto> Ausgaben)> LoadBeidesAsync(DateOnly von, DateOnly bis)
    {
        var einnahmen = await _einnahmen.GetEinnahmenAsync(von, bis);
        var ausgaben = await _ausgaben.GetAusgabenAsync(von, bis);
        return (einnahmen, ausgaben);
    }
}
