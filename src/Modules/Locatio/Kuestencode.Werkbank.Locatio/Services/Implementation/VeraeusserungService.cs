using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class VeraeusserungService : IVeraeusserungService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public VeraeusserungService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<string>> PruefeEinheitAsync(Guid einheitId, DateOnly datum)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await LadeVerkaufbareEinheitAsync(db, einheitId);
        return await SammleWarnungenAsync(db, [einheit], datum);
    }

    public async Task<List<string>> VeraeussereEinheitAsync(Guid einheitId, VeraeusserungDaten daten)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await LadeVerkaufbareEinheitAsync(db, einheitId);
        var warnungen = await SammleWarnungenAsync(db, [einheit], daten.Datum);

        einheit.Veraeusserungspreis = daten.Preis;
        einheit.Veraeusserungskosten = daten.Kosten;
        Veraeussere(einheit, daten.Datum);

        await db.SaveChangesAsync();
        return warnungen;
    }

    public async Task<List<string>> PruefeGebaeudeAsync(Guid gebaeudeId, DateOnly datum)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gebaeude = await LadeVerkaufbaresGebaeudeAsync(db, gebaeudeId);
        return await SammleWarnungenAsync(db, EigeneEinheiten(gebaeude), datum);
    }

    public async Task<List<string>> VeraeussereGebaeudeAsync(Guid gebaeudeId, VeraeusserungDaten daten)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gebaeude = await LadeVerkaufbaresGebaeudeAsync(db, gebaeudeId);
        var einheiten = EigeneEinheiten(gebaeude);
        var warnungen = await SammleWarnungenAsync(db, einheiten, daten.Datum);

        gebaeude.VerkaufsDatum = daten.Datum;
        gebaeude.Veraeusserungspreis = daten.Preis;
        gebaeude.Veraeusserungskosten = daten.Kosten;
        foreach (var einheit in einheiten)
            Veraeussere(einheit, daten.Datum);

        await db.SaveChangesAsync();
        return warnungen;
    }

    private static List<Mieteinheit> EigeneEinheiten(Gebaeude gebaeude) =>
        gebaeude.Einheiten.Where(e => e.IstEigentum).ToList();

    /// <summary>
    /// Das Eigentum endet am Tag vor dem Verkauf; ein laufendes Mietverhältnis endet für uns ebenfalls dann.
    /// </summary>
    private static void Veraeussere(Mieteinheit einheit, DateOnly datum)
    {
        if (HatLaufendesMietverhaeltnis(einheit, datum))
            einheit.VertragsEnde = datum.AddDays(-1);

        einheit.VerkaufsDatum = datum;
        einheit.Status = EinheitStatus.Veraeussert;
    }

    private static bool HatLaufendesMietverhaeltnis(Mieteinheit einheit, DateOnly datum) =>
        einheit.IstVermietet && (einheit.VertragsEnde is null || einheit.VertragsEnde >= datum);

    private static async Task<List<string>> SammleWarnungenAsync(LocatioDbContext db, List<Mieteinheit> einheiten, DateOnly datum)
    {
        var warnungen = new List<string>();
        var vertragsEnde = datum.AddDays(-1);

        foreach (var einheit in einheiten.Where(e => HatLaufendesMietverhaeltnis(e, datum)))
        {
            var bisher = einheit.VertragsEnde is { } ende ? $"bisher bis {ende:dd.MM.yyyy}" : "bisher unbefristet";
            var mieter = string.IsNullOrWhiteSpace(einheit.Mieter) ? "" : $" mit {einheit.Mieter}";
            warnungen.Add($"Das Mietverhältnis{mieter} in {einheit.Bezeichnung} wird zum {vertragsEnde:dd.MM.yyyy} beendet ({bisher}).");
        }

        var ids = einheiten.Select(e => e.Id).ToList();
        var spaetereBuchungen =
            await db.Mieteingaenge.CountAsync(m => ids.Contains(m.MieteinheitId) && m.Zahlungsdatum >= datum)
            + await db.Kostenposten.CountAsync(k => k.MieteinheitId != null && ids.Contains(k.MieteinheitId.Value) && k.Zahlungsdatum >= datum)
            + await db.Nebenkostenabrechnungen.CountAsync(n => ids.Contains(n.MieteinheitId) && n.Zahlungsdatum >= datum);
        if (spaetereBuchungen > 0)
            warnungen.Add($"Es gibt bereits {spaetereBuchungen} Buchung(en) ab dem {datum:dd.MM.yyyy}. Sie bleiben erhalten und gehen weiter an Saldo – bitte prüfen.");

        return warnungen;
    }

    private static async Task<Mieteinheit> LadeVerkaufbareEinheitAsync(LocatioDbContext db, Guid einheitId)
    {
        var einheit = await db.Mieteinheiten.Include(e => e.Gebaeude).FirstOrDefaultAsync(e => e.Id == einheitId)
            ?? throw new InvalidOperationException("Einheit nicht gefunden.");

        if (einheit.Status == EinheitStatus.Fremdeigentum)
            throw new InvalidOperationException("Fremdeigentum kann nicht veräußert werden.");
        if (einheit.Status == EinheitStatus.Veraeussert)
            throw new InvalidOperationException($"Einheit {einheit.Bezeichnung} ist bereits veräußert.");

        return einheit;
    }

    private static async Task<Gebaeude> LadeVerkaufbaresGebaeudeAsync(LocatioDbContext db, Guid gebaeudeId)
    {
        var gebaeude = await db.Gebaeude.Include(g => g.Einheiten).FirstOrDefaultAsync(g => g.Id == gebaeudeId)
            ?? throw new InvalidOperationException("Gebäude nicht gefunden.");

        if (gebaeude.VerkaufsDatum.HasValue)
            throw new InvalidOperationException($"Gebäude {gebaeude.Name} ist bereits veräußert.");

        return gebaeude;
    }
}
