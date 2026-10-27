using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class NebenkostenabrechnungService : INebenkostenabrechnungService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public NebenkostenabrechnungService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Nebenkostenabrechnung>> GetByEinheitAsync(Guid einheitId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Nebenkostenabrechnungen.AsNoTracking()
            .Where(n => n.MieteinheitId == einheitId)
            .OrderByDescending(n => n.Jahr)
            .ToListAsync();
    }

    public async Task<Nebenkostenabrechnung> SaveAsync(Nebenkostenabrechnung abrechnung, DateiUpload? pdf = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await db.Mieteinheiten.Include(e => e.Gebaeude).FirstOrDefaultAsync(e => e.Id == abrechnung.MieteinheitId)
            ?? throw new InvalidOperationException("Einheit nicht gefunden.");
        Buchungssperre.Pruefe(einheit, abrechnung.Zahlungsdatum);

        if (await db.Nebenkostenabrechnungen.AnyAsync(n =>
                n.MieteinheitId == abrechnung.MieteinheitId && n.Jahr == abrechnung.Jahr && n.Id != abrechnung.Id))
            throw new InvalidOperationException($"Für {abrechnung.Jahr} ist bereits eine Abrechnung erfasst.");

        var vorhanden = await db.Nebenkostenabrechnungen.FirstOrDefaultAsync(n => n.Id == abrechnung.Id);
        if (vorhanden == null)
        {
            if (abrechnung.Id == Guid.Empty)
                abrechnung.Id = Guid.NewGuid();
            vorhanden = new Nebenkostenabrechnung { Id = abrechnung.Id, MieteinheitId = abrechnung.MieteinheitId };
            db.Nebenkostenabrechnungen.Add(vorhanden);
        }

        vorhanden.Jahr = abrechnung.Jahr;
        vorhanden.Ergebnis = abrechnung.Ergebnis;
        vorhanden.Zahlungsdatum = abrechnung.Zahlungsdatum;
        vorhanden.Notiz = abrechnung.Notiz;

        if (pdf != null)
        {
            if (vorhanden.DokumentId is { } altesDokument)
                db.Dokumente.Remove(new Dokument { Id = altesDokument });
            var dokument = pdf.ZuDokument();
            db.Dokumente.Add(dokument);
            vorhanden.DokumentId = dokument.Id;
        }

        await db.SaveChangesAsync();
        abrechnung.DokumentId = vorhanden.DokumentId;
        return abrechnung;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var abrechnung = await db.Nebenkostenabrechnungen.FirstOrDefaultAsync(n => n.Id == id);
        if (abrechnung == null)
            return;

        db.Nebenkostenabrechnungen.Remove(abrechnung);
        if (abrechnung.DokumentId is { } dokument)
            db.Dokumente.Remove(new Dokument { Id = dokument });
        await db.SaveChangesAsync();
    }
}
