using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class KostenService : IKostenService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public KostenService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Kostenposten>> GetAsync(int jahr, Guid? gebaeudeId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Kostenposten.AsNoTracking()
            .Include(k => k.Gebaeude)
            .Include(k => k.Mieteinheit)
            .Include(k => k.Kategorie)
            .Where(k => k.Zahlungsdatum.Year == jahr && (gebaeudeId == null || k.GebaeudeId == gebaeudeId))
            .OrderByDescending(k => k.Zahlungsdatum)
            .ToListAsync();
    }

    public async Task<Kostenposten> SaveAsync(Kostenposten posten, DateiUpload? beleg = null)
    {
        if (string.IsNullOrWhiteSpace(posten.Beschreibung))
            throw new InvalidOperationException("Bitte eine Beschreibung angeben.");
        if (posten.Betrag == 0)
            throw new InvalidOperationException("Bitte den getragenen Betrag angeben.");
        if (posten.Gesamtbetrag is { } gesamt && Math.Abs(posten.Betrag) > Math.Abs(gesamt))
            throw new InvalidOperationException("Der getragene Anteil ist größer als der Gesamtbetrag.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        var kategorie = await db.Kostenkategorien.FirstOrDefaultAsync(k => k.Id == posten.KategorieId)
            ?? throw new InvalidOperationException("Bitte eine Kategorie wählen.");
        if (!KategorieTypExtensions.Ausgabetypen.Contains(kategorie.Typ))
            throw new InvalidOperationException($"Auf die Kategorie \"{kategorie.Name}\" können keine Kosten gebucht werden.");

        await PruefeZuordnungAsync(db, posten);

        var vorhanden = await db.Kostenposten.FirstOrDefaultAsync(k => k.Id == posten.Id);
        if (vorhanden == null)
        {
            if (posten.Id == Guid.Empty)
                posten.Id = Guid.NewGuid();
            vorhanden = new Kostenposten { Id = posten.Id };
            db.Kostenposten.Add(vorhanden);
        }

        vorhanden.GebaeudeId = posten.GebaeudeId;
        vorhanden.MieteinheitId = posten.MieteinheitId;
        vorhanden.KategorieId = posten.KategorieId;
        vorhanden.Betrag = posten.Betrag;
        vorhanden.Gesamtbetrag = posten.Gesamtbetrag;
        vorhanden.Verteilschluessel = posten.Verteilschluessel;
        vorhanden.Zahlungsdatum = posten.Zahlungsdatum;
        vorhanden.Beschreibung = posten.Beschreibung.Trim();
        vorhanden.Notiz = posten.Notiz;

        if (beleg != null)
        {
            if (vorhanden.BelegId is { } alterBeleg)
                db.Dokumente.Remove(new Dokument { Id = alterBeleg });
            var dokument = beleg.ZuDokument();
            db.Dokumente.Add(dokument);
            vorhanden.BelegId = dokument.Id;
        }

        await db.SaveChangesAsync();
        posten.BelegId = vorhanden.BelegId;
        return posten;
    }

    /// <summary>
    /// Bei Zuordnung zu einer Einheit wird deren Gebäude mitgeführt, damit Gebäudeauswertungen die Kosten enthalten.
    /// </summary>
    private static async Task PruefeZuordnungAsync(LocatioDbContext db, Kostenposten posten)
    {
        if (posten.MieteinheitId is { } einheitId)
        {
            var einheit = await db.Mieteinheiten.Include(e => e.Gebaeude).FirstOrDefaultAsync(e => e.Id == einheitId)
                ?? throw new InvalidOperationException("Einheit nicht gefunden.");
            Buchungssperre.Pruefe(einheit, posten.Zahlungsdatum);
            posten.GebaeudeId = einheit.GebaeudeId;
            return;
        }

        if (posten.GebaeudeId is not { } gebaeudeId)
            throw new InvalidOperationException("Bitte ein Gebäude oder eine Einheit zuordnen.");

        var gebaeude = await db.Gebaeude.FirstOrDefaultAsync(g => g.Id == gebaeudeId)
            ?? throw new InvalidOperationException("Gebäude nicht gefunden.");
        Buchungssperre.Pruefe(gebaeude, posten.Zahlungsdatum);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var posten = await db.Kostenposten.FirstOrDefaultAsync(k => k.Id == id);
        if (posten == null)
            return;

        db.Kostenposten.Remove(posten);
        if (posten.BelegId is { } beleg)
            db.Dokumente.Remove(new Dokument { Id = beleg });
        await db.SaveChangesAsync();
    }
}
