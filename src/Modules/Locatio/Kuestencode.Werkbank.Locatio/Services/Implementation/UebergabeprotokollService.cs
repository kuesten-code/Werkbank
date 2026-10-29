using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class UebergabeprotokollService : IUebergabeprotokollService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public UebergabeprotokollService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Uebergabeprotokoll>> GetByEinheitAsync(Guid einheitId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Uebergabeprotokolle.AsNoTracking()
            .Where(p => p.MieteinheitId == einheitId)
            .OrderByDescending(p => p.Datum)
            .ToListAsync();
    }

    public async Task<Uebergabeprotokoll> SaveAsync(Uebergabeprotokoll protokoll, DateiUpload? datei)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if (!await db.Mieteinheiten.AnyAsync(e => e.Id == protokoll.MieteinheitId))
            throw new InvalidOperationException("Einheit nicht gefunden.");

        var vorhanden = await db.Uebergabeprotokolle.FirstOrDefaultAsync(p => p.Id == protokoll.Id);
        if (vorhanden == null)
        {
            if (datei == null)
                throw new InvalidOperationException("Bitte das Übergabeprotokoll als Datei anhängen.");

            if (protokoll.Id == Guid.Empty)
                protokoll.Id = Guid.NewGuid();
            vorhanden = new Uebergabeprotokoll { Id = protokoll.Id, MieteinheitId = protokoll.MieteinheitId };
            db.Uebergabeprotokolle.Add(vorhanden);
        }

        vorhanden.Art = protokoll.Art;
        vorhanden.Datum = protokoll.Datum;
        vorhanden.Mieter = string.IsNullOrWhiteSpace(protokoll.Mieter) ? null : protokoll.Mieter.Trim();
        vorhanden.Notiz = protokoll.Notiz;

        if (datei != null)
        {
            var altesDokument = vorhanden.DokumentId;
            var dokument = datei.ZuDokument();
            db.Dokumente.Add(dokument);
            vorhanden.DokumentId = dokument.Id;
            if (altesDokument != Guid.Empty)
                db.Dokumente.Remove(new Dokument { Id = altesDokument });
        }

        await db.SaveChangesAsync();
        protokoll.DokumentId = vorhanden.DokumentId;
        return protokoll;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var protokoll = await db.Uebergabeprotokolle.FirstOrDefaultAsync(p => p.Id == id);
        if (protokoll == null)
            return;

        db.Uebergabeprotokolle.Remove(protokoll);
        db.Dokumente.Remove(new Dokument { Id = protokoll.DokumentId });
        await db.SaveChangesAsync();
    }
}
