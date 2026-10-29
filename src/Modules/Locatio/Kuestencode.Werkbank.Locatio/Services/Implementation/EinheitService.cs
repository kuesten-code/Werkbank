using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class EinheitService : IEinheitService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public EinheitService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<Mieteinheit?> GetAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Mieteinheiten
            .AsNoTracking()
            .Include(e => e.Gebaeude)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Mieteinheit> SaveAsync(Mieteinheit einheit)
    {
        if (string.IsNullOrWhiteSpace(einheit.Bezeichnung))
            throw new InvalidOperationException("Bitte eine Bezeichnung für die Einheit angeben.");

        einheit.Bezeichnung = einheit.Bezeichnung.Trim();
        if (einheit.Art == EinheitArt.Wohnung)
            einheit.UstOption = false;
        if (einheit.VertragsBeginn > einheit.VertragsEnde)
            throw new InvalidOperationException("Das Vertragsende liegt vor dem Vertragsbeginn.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        if (einheit.Status == EinheitStatus.Fremdeigentum)
        {
            if (await HatBuchungenAsync(db, einheit.Id))
                throw new InvalidOperationException(
                    "Die Einheit hat bereits Buchungen und kann nicht als Fremdeigentum markiert werden.");
            EntferneEigentumsdaten(einheit);
        }

        var vorhanden = await db.Mieteinheiten.FirstOrDefaultAsync(e => e.Id == einheit.Id);
        if (vorhanden == null)
        {
            if (einheit.Status == EinheitStatus.Veraeussert || einheit.VerkaufsDatum.HasValue)
                throw new InvalidOperationException("Ein Verkauf wird über \"Einheit veräußern\" erfasst.");
            if (!await db.Gebaeude.AnyAsync(g => g.Id == einheit.GebaeudeId))
                throw new InvalidOperationException("Das Gebäude der Einheit wurde nicht gefunden.");

            if (einheit.Id == Guid.Empty)
                einheit.Id = Guid.NewGuid();
            einheit.Gebaeude = null;
            einheit.Grundriss = null;
            db.Mieteinheiten.Add(einheit);
        }
        else
        {
            var veraeusserungGeaendert = (vorhanden.Status == EinheitStatus.Veraeussert) != (einheit.Status == EinheitStatus.Veraeussert)
                || vorhanden.VerkaufsDatum != einheit.VerkaufsDatum;
            if (veraeusserungGeaendert)
                throw new InvalidOperationException("Status \"veräußert\" und Verkaufsdatum werden über \"Einheit veräußern\" gesetzt.");

            db.Entry(vorhanden).CurrentValues.SetValues(einheit);
        }

        await db.SaveChangesAsync();
        return einheit;
    }

    public async Task SetGrundrissAsync(Guid einheitId, DateiUpload datei)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await db.Mieteinheiten.FirstOrDefaultAsync(e => e.Id == einheitId)
            ?? throw new InvalidOperationException("Einheit nicht gefunden.");

        if (einheit.GrundrissId is { } alterGrundriss)
            db.Dokumente.Remove(new Dokument { Id = alterGrundriss });

        var dokument = datei.ZuDokument();
        db.Dokumente.Add(dokument);
        einheit.GrundrissId = dokument.Id;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await db.Mieteinheiten.FirstOrDefaultAsync(e => e.Id == id);
        if (einheit == null)
            return;

        if (await HatBuchungenAsync(db, id))
            throw new InvalidOperationException(
                "Die Einheit hat Buchungen und kann nicht gelöscht werden. Verkaufte Einheiten bleiben archiviert.");

        db.Mieteinheiten.Remove(einheit);
        await db.SaveChangesAsync();
    }

    private static async Task<bool> HatBuchungenAsync(LocatioDbContext db, Guid einheitId) =>
        await db.Mieteingaenge.AnyAsync(m => m.MieteinheitId == einheitId)
        || await db.Kostenposten.AnyAsync(k => k.MieteinheitId == einheitId)
        || await db.Nebenkostenabrechnungen.AnyAsync(n => n.MieteinheitId == einheitId);

    private static void EntferneEigentumsdaten(Mieteinheit einheit)
    {
        einheit.AnschaffungsDatum = null;
        einheit.AnschaffungskostenGebaeude = null;
        einheit.AnschaffungskostenGrundUndBoden = null;
        einheit.Anschaffungsnebenkosten = null;
        einheit.Veraeusserungspreis = null;
        einheit.Veraeusserungskosten = null;
        einheit.Kaltmiete = 0;
        einheit.NebenkostenVorauszahlung = 0;
        einheit.Mieter = null;
        einheit.VertragsBeginn = null;
        einheit.VertragsEnde = null;
        einheit.UstOption = false;
    }
}
