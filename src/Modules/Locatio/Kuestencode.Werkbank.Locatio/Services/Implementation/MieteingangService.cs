using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class MieteingangService : IMieteingangService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public MieteingangService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Mieteingang>> GetAsync(int jahr, Guid? einheitId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Mieteingaenge.AsNoTracking()
            .Include(m => m.Mieteinheit).ThenInclude(e => e!.Gebaeude)
            .Where(m => m.Zahlungsdatum.Year == jahr && (einheitId == null || m.MieteinheitId == einheitId))
            .OrderByDescending(m => m.Zahlungsdatum)
            .ToListAsync();
    }

    public async Task<Mieteingang> SaveAsync(Mieteingang eingang)
    {
        if (eingang.Netto <= 0)
            throw new InvalidOperationException("Bitte den eingegangenen Betrag angeben.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await db.Mieteinheiten.Include(e => e.Gebaeude).FirstOrDefaultAsync(e => e.Id == eingang.MieteinheitId)
            ?? throw new InvalidOperationException("Einheit nicht gefunden.");
        Buchungssperre.Pruefe(einheit, eingang.Zahlungsdatum);

        eingang.Monat = new DateOnly(eingang.Monat.Year, eingang.Monat.Month, 1);

        var vorhanden = await db.Mieteingaenge.FirstOrDefaultAsync(m => m.Id == eingang.Id);
        if (vorhanden == null)
        {
            if (eingang.Id == Guid.Empty)
                eingang.Id = Guid.NewGuid();
            eingang.UstSatz = einheit.WirksamerUstSatz;
            eingang.Mieteinheit = null;
            db.Mieteingaenge.Add(eingang);
        }
        else
        {
            vorhanden.MieteinheitId = eingang.MieteinheitId;
            vorhanden.Zahlungsdatum = eingang.Zahlungsdatum;
            vorhanden.Monat = eingang.Monat;
            vorhanden.Kaltmiete = eingang.Kaltmiete;
            vorhanden.Nebenkosten = eingang.Nebenkosten;
            vorhanden.Notiz = eingang.Notiz;
        }

        await db.SaveChangesAsync();
        return eingang;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var eingang = await db.Mieteingaenge.FirstOrDefaultAsync(m => m.Id == id);
        if (eingang == null)
            return;

        db.Mieteingaenge.Remove(eingang);
        await db.SaveChangesAsync();
    }

    public async Task<int> ErfasseMonatAsync(DateOnly monat, DateOnly zahlungsdatum)
    {
        var ersterTag = new DateOnly(monat.Year, monat.Month, 1);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var bereitsErfasst = await db.Mieteingaenge
            .Where(m => m.Monat == ersterTag)
            .Select(m => m.MieteinheitId)
            .ToListAsync();

        var einheiten = (await db.Mieteinheiten.Include(e => e.Gebaeude).ToListAsync())
            .Where(e => e.IstEigentum && e.IstVermietet && e.MonatsmieteNetto > 0)
            .Where(e => e.GesperrtAb is not { } gesperrt || zahlungsdatum < gesperrt)
            .Where(e => !bereitsErfasst.Contains(e.Id));

        var anzahl = 0;
        foreach (var einheit in einheiten)
        {
            db.Mieteingaenge.Add(new Mieteingang
            {
                Id = Guid.NewGuid(),
                MieteinheitId = einheit.Id,
                Zahlungsdatum = zahlungsdatum,
                Monat = ersterTag,
                Kaltmiete = einheit.Kaltmiete,
                Nebenkosten = einheit.NebenkostenVorauszahlung,
                UstSatz = einheit.WirksamerUstSatz
            });
            anzahl++;
        }

        await db.SaveChangesAsync();
        return anzahl;
    }
}
