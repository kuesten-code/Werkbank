using Kuestencode.Shared.Contracts.Locatio;
using Kuestencode.Werkbank.Locatio.Data;
using Kuestencode.Werkbank.Locatio.Domain.Entities;
using Kuestencode.Werkbank.Locatio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Locatio.Services;

/// <summary>
/// Übersetzt die erfassten Vorgänge in Saldo-Buchungen nach Zahlungsdatum.
/// Tilgung ist keine Werbungskosten und wird nie übergeben; Kosten gehen nur mit dem getragenen Anteil ein.
/// </summary>
public class SaldoBuchungService : ISaldoBuchungService
{
    private readonly IDbContextFactory<LocatioDbContext> _dbFactory;

    public SaldoBuchungService(IDbContextFactory<LocatioDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<LocatioBuchungDto>> GetBuchungenAsync(DateOnly von, DateOnly bis) =>
        (await GetBuchungenMitGebaeudeAsync(von, bis)).Select(b => b.Buchung).ToList();

    public async Task<List<(Guid GebaeudeId, LocatioBuchungDto Buchung)>> GetBuchungenMitGebaeudeAsync(DateOnly von, DateOnly bis)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var kategorien = await db.Kostenkategorien.AsNoTracking().OrderBy(k => k.Sortierung).ToListAsync();
        var buchungen = new List<(Guid, LocatioBuchungDto)>();

        var mieteingaenge = await db.Mieteingaenge.AsNoTracking()
            .Include(m => m.Mieteinheit)
            .Where(m => m.Zahlungsdatum >= von && m.Zahlungsdatum <= bis)
            .ToListAsync();
        var mietKategorie = SystemKategorie(kategorien, KategorieTyp.Mieteinnahme);
        buchungen.AddRange(mieteingaenge.Select(m => (m.Mieteinheit!.GebaeudeId, Mietbuchung(m, mietKategorie))));

        var kostenposten = await db.Kostenposten.AsNoTracking()
            .Include(k => k.Kategorie)
            .Include(k => k.Mieteinheit)
            .Where(k => k.Zahlungsdatum >= von && k.Zahlungsdatum <= bis && k.Kategorie!.Typ != KategorieTyp.Tilgung)
            .ToListAsync();
        buchungen.AddRange(kostenposten.Select(k => (k.GebaeudeId ?? k.Mieteinheit!.GebaeudeId, Kostenbuchung(k))));

        var zinszahlungen = await db.Darlehenszahlungen.AsNoTracking()
            .Include(z => z.Darlehen)
            .Where(z => z.Zahlungsdatum >= von && z.Zahlungsdatum <= bis && z.Zinsanteil != 0)
            .ToListAsync();
        var zinsKategorie = SystemKategorie(kategorien, KategorieTyp.Darlehenszinsen);
        buchungen.AddRange(zinszahlungen.Select(z => (z.Darlehen!.GebaeudeId, Zinsbuchung(z, zinsKategorie))));

        var abrechnungen = await db.Nebenkostenabrechnungen.AsNoTracking()
            .Include(n => n.Mieteinheit)
            .Where(n => n.Zahlungsdatum >= von && n.Zahlungsdatum <= bis && n.Ergebnis != 0)
            .ToListAsync();
        var nkKategorie = SystemKategorie(kategorien, KategorieTyp.Nebenkostenabrechnung);
        buchungen.AddRange(abrechnungen.Select(n => (n.Mieteinheit!.GebaeudeId, NkBuchung(n, nkKategorie))));

        return buchungen.OrderBy(b => b.Item2.Zahlungsdatum).ToList();
    }

    private static Kostenkategorie SystemKategorie(List<Kostenkategorie> kategorien, KategorieTyp typ) =>
        kategorien.FirstOrDefault(k => k.Typ == typ)
        ?? throw new InvalidOperationException($"Es gibt keine Kategorie vom Typ {typ.Anzeigename()}.");

    private static LocatioBuchungDto Mietbuchung(Mieteingang m, Kostenkategorie kategorie) => new()
    {
        Id = m.Id,
        Referenz = $"Miete {m.Mieteinheit!.Bezeichnung} {m.Monat:MM/yyyy}",
        Zahlungsdatum = m.Zahlungsdatum,
        IstEinnahme = true,
        Beschreibung = m.Mieteinheit.Mieter ?? m.Mieteinheit.Bezeichnung,
        Netto = m.Netto,
        Ust = m.Ust,
        Brutto = m.Brutto,
        UstSatz = m.UstSatz,
        Kategorie = kategorie.Name,
        KontoNummer = kategorie.KontoNummer
    };

    private static LocatioBuchungDto Kostenbuchung(Kostenposten k) => new()
    {
        Id = k.Id,
        Referenz = k.Mieteinheit is { } einheit ? $"Kosten {einheit.Bezeichnung}" : "Kosten Gebäude",
        Zahlungsdatum = k.Zahlungsdatum,
        IstEinnahme = false,
        Beschreibung = k.Beschreibung,
        Netto = k.Betrag,
        Brutto = k.Betrag,
        Kategorie = k.Kategorie!.Name,
        KontoNummer = k.Kategorie.KontoNummer
    };

    private static LocatioBuchungDto Zinsbuchung(Darlehenszahlung z, Kostenkategorie kategorie) => new()
    {
        Id = z.Id,
        Referenz = $"Zins {z.Darlehen!.Bezeichnung}",
        Zahlungsdatum = z.Zahlungsdatum,
        IstEinnahme = false,
        Beschreibung = z.Darlehen.Kreditgeber ?? z.Darlehen.Bezeichnung,
        Netto = z.Zinsanteil,
        Brutto = z.Zinsanteil,
        Kategorie = kategorie.Name,
        KontoNummer = kategorie.KontoNummer
    };

    /// <summary>
    /// Nachzahlung = Einnahme, Erstattung = Ausgabe. Bei Gewerbe mit USt-Option ist das Ergebnis brutto.
    /// </summary>
    private static LocatioBuchungDto NkBuchung(Nebenkostenabrechnung n, Kostenkategorie kategorie)
    {
        var brutto = Math.Abs(n.Ergebnis);
        var ustSatz = n.Mieteinheit!.WirksamerUstSatz;
        var netto = Math.Round(brutto / (1 + ustSatz / 100m), 2);

        return new LocatioBuchungDto
        {
            Id = n.Id,
            Referenz = $"NK-Abrechnung {n.Mieteinheit.Bezeichnung} {n.Jahr}",
            Zahlungsdatum = n.Zahlungsdatum,
            IstEinnahme = n.Ergebnis > 0,
            Beschreibung = n.Ergebnis > 0 ? "Nachzahlung Mieter" : "Erstattung an Mieter",
            Netto = netto,
            Ust = brutto - netto,
            Brutto = brutto,
            UstSatz = ustSatz,
            Kategorie = kategorie.Name,
            KontoNummer = kategorie.KontoNummer
        };
    }
}
