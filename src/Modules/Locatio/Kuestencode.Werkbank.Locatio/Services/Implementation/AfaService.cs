using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class AfaService : IAfaService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public AfaService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<AfaVorschlagDto>> GetVorschlaegeAsync(int jahr)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheiten = await db.Mieteinheiten.AsNoTrackingWithIdentityResolution()
            .Include(e => e.Gebaeude!.Einheiten)
            .Where(e => e.Status != EinheitStatus.Fremdeigentum)
            .ToListAsync();
        var afaPosten = await LadeAfaPostenAsync(db, einheiten.Select(e => e.Id).ToList());

        return einheiten
            .Select(e => (Einheit: e, Grundlage: AfaRechner.Grundlage(e)))
            .Where(x => x.Grundlage is { AfaBasis: > 0 })
            .Select(x =>
            {
                var posten = afaPosten.Where(p => p.MieteinheitId == x.Einheit.Id).ToList();
                var angesetztVorJahr = posten.Where(p => p.Zahlungsdatum.Year < jahr).Sum(p => p.Betrag);
                var (monate, betrag, datum) = AfaRechner.Berechne(x.Einheit, jahr, angesetztVorJahr);
                return new AfaVorschlagDto
                {
                    MieteinheitId = x.Einheit.Id,
                    GebaeudeId = x.Einheit.GebaeudeId,
                    Gebaeude = x.Einheit.Gebaeude?.Name ?? string.Empty,
                    Einheit = x.Einheit.Bezeichnung,
                    AfaBasis = x.Grundlage!.AfaBasis,
                    AfaSatz = x.Grundlage.AfaSatz,
                    AusGebaeude = x.Grundlage.AusGebaeude,
                    Monate = monate,
                    BereitsAngesetzt = angesetztVorJahr,
                    Betrag = betrag,
                    Buchungsdatum = datum,
                    BereitsGebucht = posten.Any(p => p.Zahlungsdatum.Year == jahr)
                };
            })
            .Where(v => v.Monate > 0)
            .OrderBy(v => v.Gebaeude).ThenBy(v => v.Einheit)
            .ToList();
    }

    public async Task BucheAsync(Guid einheitId, int jahr)
    {
        var vorschlag = (await GetVorschlaegeAsync(jahr)).FirstOrDefault(v => v.MieteinheitId == einheitId)
            ?? throw new InvalidOperationException($"Für diese Einheit ist {jahr} keine AfA anzusetzen.");
        if (vorschlag.BereitsGebucht)
            throw new InvalidOperationException($"Die AfA {jahr} für {vorschlag.Einheit} ist bereits gebucht.");
        if (vorschlag.Betrag <= 0)
            throw new InvalidOperationException($"Die AfA-Basis von {vorschlag.Einheit} ist bereits vollständig abgeschrieben.");

        await BucheAsync([vorschlag], jahr);
    }

    public async Task<int> BucheGebaeudeAsync(Guid gebaeudeId, int jahr)
    {
        var offen = (await GetVorschlaegeAsync(jahr))
            .Where(v => v.GebaeudeId == gebaeudeId && !v.BereitsGebucht && v.Betrag > 0)
            .ToList();
        if (offen.Count == 0)
            throw new InvalidOperationException($"Für {jahr} ist in diesem Gebäude keine AfA mehr offen.");

        await BucheAsync(offen, jahr);
        return offen.Count;
    }

    private async Task BucheAsync(List<AfaVorschlagDto> vorschlaege, int jahr)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kategorie = await db.Kostenkategorien.OrderBy(k => k.Sortierung).FirstOrDefaultAsync(k => k.Typ == KategorieTyp.Afa)
            ?? throw new InvalidOperationException("Es gibt keine Kategorie vom Typ AfA.");

        db.Kostenposten.AddRange(vorschlaege.Select(v => new Kostenposten
        {
            Id = Guid.NewGuid(),
            GebaeudeId = v.GebaeudeId,
            MieteinheitId = v.MieteinheitId,
            KategorieId = kategorie.Id,
            Betrag = v.Betrag,
            Zahlungsdatum = v.Buchungsdatum,
            Beschreibung = $"AfA {jahr} {v.Einheit} ({v.AfaSatz:0.##} % von {v.AfaBasis:N2} €{(v.AusGebaeude ? " anteilig nach Fläche" : "")}, {v.Monate} Monate)"
        }));
        await db.SaveChangesAsync();
    }

    public static async Task<List<Kostenposten>> LadeAfaPostenAsync(LocatioDbContext db, List<Guid> einheitIds) =>
        await db.Kostenposten.AsNoTracking()
            .Where(k => k.MieteinheitId != null && einheitIds.Contains(k.MieteinheitId.Value) && k.Kategorie!.Typ == KategorieTyp.Afa)
            .ToListAsync();
}
