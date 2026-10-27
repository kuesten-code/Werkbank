using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class GebaeudeService : IGebaeudeService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;
    private readonly ISaldoBuchungService _buchungen;

    public GebaeudeService(IDbContextFactory<LocatioDbContext> dbFactory, ISaldoBuchungService buchungen)
    {
        _dbFactory = dbFactory;
        _buchungen = buchungen;
    }

    public async Task<List<GebaeudeKachelDto>> GetKachelnAsync(int jahr)
    {
        var gebaeude = await GetAllAsync();
        var buchungen = await _buchungen.GetBuchungenMitGebaeudeAsync(new DateOnly(jahr, 1, 1), new DateOnly(jahr, 12, 31));

        return gebaeude.Select(g =>
        {
            var eigene = buchungen.Where(b => b.GebaeudeId == g.Id).Select(b => b.Buchung).ToList();
            return BerechneKachel(g,
                eigene.Where(b => b.IstEinnahme).Sum(b => b.Netto),
                eigene.Where(b => !b.IstEinnahme).Sum(b => b.Netto));
        }).ToList();
    }

    public static GebaeudeKachelDto BerechneKachel(Gebaeude gebaeude, decimal einnahmen, decimal werbungskosten)
    {
        var eigene = gebaeude.Einheiten.Where(e => e.IstEigentum).ToList();
        var vermietet = eigene.Where(e => e.IstVermietet).ToList();

        return new GebaeudeKachelDto
        {
            Id = gebaeude.Id,
            Name = gebaeude.Name,
            Adresse = gebaeude.Adresse,
            AbrechnerUrl = gebaeude.AbrechnerUrl,
            EinheitenGesamt = gebaeude.Einheiten.Count,
            EinheitenEigentum = eigene.Count,
            EinheitenVermietet = vermietet.Count,
            EinheitenLeer = eigene.Count - vermietet.Count,
            VerkaufsDatum = gebaeude.VerkaufsDatum,
            SollmieteMonat = vermietet.Sum(e => e.MonatsmieteNetto),
            EinnahmenJahr = einnahmen,
            WerbungskostenJahr = werbungskosten
        };
    }

    public async Task<List<Gebaeude>> GetAllAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Gebaeude
            .AsNoTracking()
            .Include(g => g.Einheiten.OrderBy(e => e.Bezeichnung))
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<Gebaeude?> GetAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Gebaeude
            .AsNoTracking()
            .Include(g => g.Einheiten.OrderBy(e => e.Bezeichnung))
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Gebaeude> SaveAsync(Gebaeude gebaeude)
    {
        if (string.IsNullOrWhiteSpace(gebaeude.Name))
            throw new InvalidOperationException("Bitte einen Namen für das Gebäude angeben.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        gebaeude.Name = gebaeude.Name.Trim();
        var vorhanden = await db.Gebaeude.FirstOrDefaultAsync(g => g.Id == gebaeude.Id);

        if (vorhanden == null)
        {
            if (gebaeude.VerkaufsDatum.HasValue)
                throw new InvalidOperationException("Ein Verkauf wird über \"Gebäude veräußern\" erfasst.");

            if (gebaeude.Id == Guid.Empty)
                gebaeude.Id = Guid.NewGuid();
            db.Gebaeude.Add(gebaeude);
        }
        else
        {
            if (vorhanden.VerkaufsDatum != gebaeude.VerkaufsDatum)
                throw new InvalidOperationException("Das Verkaufsdatum wird über \"Gebäude veräußern\" geändert.");

            db.Entry(vorhanden).CurrentValues.SetValues(gebaeude);
        }

        await db.SaveChangesAsync();
        return gebaeude;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gebaeude = await db.Gebaeude.FirstOrDefaultAsync(g => g.Id == id);
        if (gebaeude == null)
            return;

        var inVerwendung = await db.Mieteinheiten.AnyAsync(e => e.GebaeudeId == id)
            || await db.Kostenposten.AnyAsync(k => k.GebaeudeId == id)
            || await db.Darlehen.AnyAsync(d => d.GebaeudeId == id);
        if (inVerwendung)
            throw new InvalidOperationException(
                "Das Gebäude hat Einheiten, Kosten oder Darlehen und kann nicht gelöscht werden. Verkaufte Gebäude bleiben archiviert.");

        db.Gebaeude.Remove(gebaeude);
        await db.SaveChangesAsync();
    }
}
