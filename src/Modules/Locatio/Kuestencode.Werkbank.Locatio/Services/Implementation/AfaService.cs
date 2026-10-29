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
        var einheiten = await db.Mieteinheiten.AsNoTracking()
            .Include(e => e.Gebaeude)
            .Where(e => e.Status != EinheitStatus.Fremdeigentum && e.AnschaffungsDatum != null)
            .ToListAsync();
        var afaPosten = await LadeAfaPostenAsync(db, einheiten.Select(e => e.Id).ToList());

        return einheiten
            .Where(e => e.AfaBasis > 0)
            .Select(e =>
            {
                var posten = afaPosten.Where(p => p.MieteinheitId == e.Id).ToList();
                var angesetztVorJahr = posten.Where(p => p.Zahlungsdatum.Year < jahr).Sum(p => p.Betrag);
                var (monate, betrag, datum) = AfaRechner.Berechne(e, jahr, angesetztVorJahr);
                return new AfaVorschlagDto
                {
                    MieteinheitId = e.Id,
                    Gebaeude = e.Gebaeude?.Name ?? string.Empty,
                    Einheit = e.Bezeichnung,
                    AfaBasis = e.AfaBasis,
                    AfaSatz = e.AfaSatz,
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

        await using var db = await _dbFactory.CreateDbContextAsync();
        var kategorie = await db.Kostenkategorien.OrderBy(k => k.Sortierung).FirstOrDefaultAsync(k => k.Typ == KategorieTyp.Afa)
            ?? throw new InvalidOperationException("Es gibt keine Kategorie vom Typ AfA.");
        var einheit = await db.Mieteinheiten.FirstAsync(e => e.Id == einheitId);

        db.Kostenposten.Add(new Kostenposten
        {
            Id = Guid.NewGuid(),
            GebaeudeId = einheit.GebaeudeId,
            MieteinheitId = einheitId,
            KategorieId = kategorie.Id,
            Betrag = vorschlag.Betrag,
            Zahlungsdatum = vorschlag.Buchungsdatum,
            Beschreibung = $"AfA {jahr} {vorschlag.Einheit} ({vorschlag.AfaSatz:0.##} % von {vorschlag.AfaBasis:N2} €, {vorschlag.Monate} Monate)"
        });
        await db.SaveChangesAsync();
    }

    public static async Task<List<Kostenposten>> LadeAfaPostenAsync(LocatioDbContext db, List<Guid> einheitIds) =>
        await db.Kostenposten.AsNoTracking()
            .Where(k => k.MieteinheitId != null && einheitIds.Contains(k.MieteinheitId.Value) && k.Kategorie!.Typ == KategorieTyp.Afa)
            .ToListAsync();
}
