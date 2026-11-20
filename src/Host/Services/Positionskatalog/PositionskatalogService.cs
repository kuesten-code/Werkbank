using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Positionskatalog;

public enum PositionsvorlageSpeichern
{
    Gespeichert,
    Ungueltig,
    Stundeneinheit,
    Dublette,
    NichtGefunden
}

public interface IPositionskatalogService
{
    Task<List<Positionsvorlage>> SucheAsync(string? suche, int limit);
    Task<List<string>> GetEinheitenAsync();
    Task MerkenAsync(IEnumerable<PositionsvorlageMerkenDto> positionen);
    Task<List<Positionsvorlage>> AlleAsync();
    Task<PositionsvorlageSpeichern> SpeichernAsync(int? id, string? text, string? einheit, decimal einzelpreis);
    Task LoeschenAsync(int id);
}

public class PositionskatalogService : IPositionskatalogService
{
    public const int MaxTextLaenge = 1000;
    public const int MaxEinheitLaenge = 20;

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
            .Where(p => Pruefen(p.Text, p.Einheit, p.Einzelpreis) == PositionsvorlageSpeichern.Gespeichert)
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
                vorlage = new Positionsvorlage();
                _db.Positionsvorlagen.Add(vorlage);
            }
            else if (datum < vorlage.ZuletztVerwendetAm)
            {
                continue;
            }

            Uebernehmen(vorlage, key, position.Text, position.Einheit, position.Einzelpreis, datum);
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<Positionsvorlage>> AlleAsync()
    {
        return await _db.Positionsvorlagen
            .AsNoTracking()
            .OrderBy(v => v.Text)
            .ThenBy(v => v.Einheit)
            .ToListAsync();
    }

    public async Task<PositionsvorlageSpeichern> SpeichernAsync(int? id, string? text, string? einheit, decimal einzelpreis)
    {
        var pruefung = Pruefen(text, einheit, einzelpreis);
        if (pruefung != PositionsvorlageSpeichern.Gespeichert)
            return pruefung;

        var schluessel = PositionsNormalisierer.Schluessel(text!, einheit);
        if (await _db.Positionsvorlagen.AnyAsync(v => v.Schluessel == schluessel && v.Id != id))
            return PositionsvorlageSpeichern.Dublette;

        Positionsvorlage? vorlage;
        if (id is null)
        {
            vorlage = new Positionsvorlage();
            _db.Positionsvorlagen.Add(vorlage);
        }
        else
        {
            vorlage = await _db.Positionsvorlagen.FindAsync(id.Value);
            if (vorlage is null)
                return PositionsvorlageSpeichern.NichtGefunden;
        }

        // Ein von Hand gepflegter Preis gilt, bis ein neueres Dokument ihn überschreibt.
        Uebernehmen(vorlage, schluessel, text!, einheit, einzelpreis, DateTime.UtcNow);
        await _db.SaveChangesAsync();
        return PositionsvorlageSpeichern.Gespeichert;
    }

    public async Task LoeschenAsync(int id)
    {
        var vorlage = await _db.Positionsvorlagen.FindAsync(id);
        if (vorlage is null)
            return;

        _db.Positionsvorlagen.Remove(vorlage);
        await _db.SaveChangesAsync();
    }

    private static void Uebernehmen(Positionsvorlage vorlage, string schluessel, string text, string? einheit, decimal einzelpreis, DateTime datum)
    {
        vorlage.Schluessel = schluessel;
        vorlage.Text = text.Trim();
        vorlage.Einheit = string.IsNullOrWhiteSpace(einheit) ? null : einheit.Trim();
        vorlage.Einzelpreis = einzelpreis;
        vorlage.ZuletztVerwendetAm = datum;
    }

    private static PositionsvorlageSpeichern Pruefen(string? text, string? einheit, decimal einzelpreis)
    {
        if (string.IsNullOrWhiteSpace(text)
            || text.Trim().Length > MaxTextLaenge
            || (einheit?.Trim().Length ?? 0) > MaxEinheitLaenge
            || einzelpreis < 0)
            return PositionsvorlageSpeichern.Ungueltig;

        return PositionsNormalisierer.IstStundenEinheit(einheit)
            ? PositionsvorlageSpeichern.Stundeneinheit
            : PositionsvorlageSpeichern.Gespeichert;
    }
}
