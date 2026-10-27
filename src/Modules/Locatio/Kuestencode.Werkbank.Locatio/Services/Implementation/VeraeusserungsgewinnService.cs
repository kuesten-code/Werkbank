using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Dtos;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

public class VeraeusserungsgewinnService : IVeraeusserungsgewinnService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public VeraeusserungsgewinnService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<VeraeusserungsgewinnDto?> FuerEinheitAsync(Guid einheitId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var einheit = await db.Mieteinheiten.AsNoTracking().Include(e => e.Gebaeude).FirstOrDefaultAsync(e => e.Id == einheitId);
        if (einheit == null)
            return null;

        var afa = await SummeAfaAsync(db, k => k.MieteinheitId == einheitId);
        return Berechne($"{einheit.Gebaeude?.Name} – {einheit.Bezeichnung}", einheit, einheit.VerkaufsDatum ?? einheit.Gebaeude?.VerkaufsDatum, afa);
    }

    public async Task<VeraeusserungsgewinnDto?> FuerGebaeudeAsync(Guid gebaeudeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var gebaeude = await db.Gebaeude.AsNoTracking().Include(g => g.Einheiten).FirstOrDefaultAsync(g => g.Id == gebaeudeId);
        if (gebaeude == null)
            return null;

        var afa = await SummeAfaAsync(db, k => k.GebaeudeId == gebaeudeId);
        var dto = Berechne(gebaeude.Name, gebaeude, gebaeude.VerkaufsDatum, afa);

        // Wurden die Anschaffungsdaten nur je Einheit gepflegt, ergibt sich das Gebäude aus seinen eigenen Einheiten.
        if (gebaeude.AnschaffungskostenGesamt == 0)
        {
            var eigene = gebaeude.Einheiten.Where(e => e.Status != EinheitStatus.Fremdeigentum).ToList();
            dto.Anschaffungskosten = eigene.Sum(e => e.AnschaffungskostenGesamt);
            dto.AnschaffungsDatum ??= eigene.Min(e => e.AnschaffungsDatum);
            dto.HaltedauerMonate = Haltedauer(dto.AnschaffungsDatum, dto.VerkaufsDatum);
        }

        return dto;
    }

    public static VeraeusserungsgewinnDto Berechne(string objekt, Eigentumsobjekt quelle, DateOnly? verkaufsDatum, decimal kumulierteAfa) => new()
    {
        Objekt = objekt,
        AnschaffungsDatum = quelle.AnschaffungsDatum,
        VerkaufsDatum = verkaufsDatum,
        HaltedauerMonate = Haltedauer(quelle.AnschaffungsDatum, verkaufsDatum),
        Veraeusserungspreis = quelle.Veraeusserungspreis ?? 0,
        Veraeusserungskosten = quelle.Veraeusserungskosten ?? 0,
        Anschaffungskosten = quelle.AnschaffungskostenGesamt,
        KumulierteAfa = kumulierteAfa
    };

    public static int? Haltedauer(DateOnly? anschaffung, DateOnly? verkauf)
    {
        if (anschaffung is not { } von || verkauf is not { } bis)
            return null;

        var monate = (bis.Year - von.Year) * 12 + bis.Month - von.Month;
        return bis.Day < von.Day ? monate - 1 : monate;
    }

    private static Task<decimal> SummeAfaAsync(LocatioDbContext db, System.Linq.Expressions.Expression<Func<Kostenposten, bool>> filter) =>
        db.Kostenposten.Where(filter).Where(k => k.Kategorie!.Typ == KategorieTyp.Afa).SumAsync(k => k.Betrag);
}
