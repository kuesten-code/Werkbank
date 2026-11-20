using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Positionskatalog;

public interface IPositionskatalogService
{
    Task<List<Positionsvorlage>> SucheAsync(string? suche, int limit);
    Task<List<string>> GetEinheitenAsync();
    Task MerkenAsync(IEnumerable<PositionsvorlageMerkenDto> positionen);
}

public class PositionskatalogService : IPositionskatalogService
{
    private const int MaxTextLaenge = 1000;
    private const int MaxEinheitLaenge = 20;

    private readonly HostDbContext _db;

    public PositionskatalogService(HostDbContext db)
    {
        _db = db;
    }

    public async Task<List<Positionsvorlage>> SucheAsync(string? suche, int limit)
    {
        var vorlagen = await _db.Positionsvorlagen.AsNoTracking().ToListAsync();
        return PositionsSuche.Suchen(vorlagen, suche, limit);
    }

    public async Task<List<string>> GetEinheitenAsync()
    {
        return await _db.Positionsvorlagen
            .Where(v => v.Einheit != null && v.Einheit != "")
            .Select(v => v.Einheit!)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync();
    }

    public async Task MerkenAsync(IEnumerable<PositionsvorlageMerkenDto> positionen)
    {
        var neueste = positionen
            .Where(IstKatalogwuerdig)
            .Select(p => (Schluessel: PositionsNormalisierer.Schluessel(p.Text, p.Einheit), Position: p))
            .GroupBy(t => t.Schluessel)
            .Select(g => g.OrderByDescending(t => t.Position.Dokumentdatum).First())
            .ToList();

        if (neueste.Count == 0)
            return;

        var schluessel = neueste.Select(t => t.Schluessel).ToList();
        var vorhandene = await _db.Positionsvorlagen
            .Where(v => schluessel.Contains(v.Schluessel))
            .ToDictionaryAsync(v => v.Schluessel);

        foreach (var (key, position) in neueste)
        {
            var datum = DateTime.SpecifyKind(position.Dokumentdatum, DateTimeKind.Utc);

            if (!vorhandene.TryGetValue(key, out var vorlage))
            {
                _db.Positionsvorlagen.Add(new Positionsvorlage
                {
                    Schluessel = key,
                    Text = position.Text.Trim(),
                    Einheit = string.IsNullOrWhiteSpace(position.Einheit) ? null : position.Einheit.Trim(),
                    Einzelpreis = position.Einzelpreis,
                    ZuletztVerwendetAm = datum
                });
            }
            else if (datum >= vorlage.ZuletztVerwendetAm)
            {
                vorlage.Text = position.Text.Trim();
                vorlage.Einheit = string.IsNullOrWhiteSpace(position.Einheit) ? null : position.Einheit.Trim();
                vorlage.Einzelpreis = position.Einzelpreis;
                vorlage.ZuletztVerwendetAm = datum;
            }
        }

        await _db.SaveChangesAsync();
    }

    private static bool IstKatalogwuerdig(PositionsvorlageMerkenDto p)
        => !string.IsNullOrWhiteSpace(p.Text)
           && p.Text.Trim().Length <= MaxTextLaenge
           && (p.Einheit?.Trim().Length ?? 0) <= MaxEinheitLaenge
           && p.Einzelpreis >= 0
           && !PositionsNormalisierer.IstStundenEinheit(p.Einheit);
}
