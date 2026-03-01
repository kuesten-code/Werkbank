using Kuestencode.Werkbank.Recepta.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Saldo.Data;

/// <summary>
/// Seed-Daten für den Saldo-Kontenstamm (SKR03 + SKR04) und Standard-Kategorie-Mappings.
/// Wird beim ersten Start oder nach Migrations aufgerufen.
/// </summary>
public static class SaldoSeedData
{
    public static async Task SeedAsync(SaldoDbContext context)
    {
        await SeedKontenAsync(context);
        await SeedMappingsAsync(context);
        await SeedDefaultSettingsAsync(context);
    }

    // ─── STANDARD-EINSTELLUNGEN ────────────────────────────────────────────────

    private static async Task SeedDefaultSettingsAsync(SaldoDbContext context)
    {
        if (await context.SaldoSettings.AnyAsync()) return;

        context.SaldoSettings.Add(new SaldoSettings
        {
            Id = Guid.NewGuid(),
            Kontenrahmen = "SKR03",
            WirtschaftsjahrBeginn = 1
        });

        await context.SaveChangesAsync();
    }

    // ─── KONTEN ────────────────────────────────────────────────────────────────

    private static async Task SeedKontenAsync(SaldoDbContext context)
    {
        var existingKonten = await context.Konten.Select(k => new { k.Kontenrahmen, k.KontoNummer }).ToListAsync();
        var existingSet = existingKonten.Select(k => $"{k.Kontenrahmen}:{k.KontoNummer}").ToHashSet();

        var konten = GetSKR03Konten().Concat(GetSKR04Konten())
            .Where(k => !existingSet.Contains($"{k.Kontenrahmen}:{k.KontoNummer}"))
            .ToList();

        if (konten.Count > 0)
        {
            context.Konten.AddRange(konten);
            await context.SaveChangesAsync();
        }
    }

    private static IEnumerable<Konto> GetSKR03Konten() =>
    [
        // Einnahmen
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "8400", KontoBezeichnung = "Erlöse 19% USt",          KontoTyp = KontoTyp.Einnahme, UstSatz = 19 },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "8300", KontoBezeichnung = "Erlöse 7% USt",           KontoTyp = KontoTyp.Einnahme, UstSatz = 7  },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "8120", KontoBezeichnung = "Steuerfreie Erlöse",      KontoTyp = KontoTyp.Einnahme, UstSatz = 0  },
        // Bank
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "1200", KontoBezeichnung = "Bank",                   KontoTyp = KontoTyp.Bank },
        // Ausgaben
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "3300", KontoBezeichnung = "Wareneingang 19% Vorsteuer",    KontoTyp = KontoTyp.Ausgabe, UstSatz = 19 },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4100", KontoBezeichnung = "Löhne und Gehälter",            KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4120", KontoBezeichnung = "Sozialversicherung AG-Anteil",  KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4210", KontoBezeichnung = "Miete",                         KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4240", KontoBezeichnung = "Heizung, Strom, Wasser",        KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4500", KontoBezeichnung = "Fahrzeugkosten",                KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4660", KontoBezeichnung = "Reisekosten",                   KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4600", KontoBezeichnung = "Werbekosten",                   KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4930", KontoBezeichnung = "Bürobedarf",                    KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4964", KontoBezeichnung = "EDV-Kosten, Software",          KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4920", KontoBezeichnung = "Telefon, Internet",             KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4360", KontoBezeichnung = "Versicherungen",                KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4380", KontoBezeichnung = "Beiträge und Gebühren",         KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4900", KontoBezeichnung = "Fremdleistungen",               KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", KontoNummer = "4950", KontoBezeichnung = "Sonstige Kosten",               KontoTyp = KontoTyp.Ausgabe },
    ];

    private static IEnumerable<Konto> GetSKR04Konten() =>
    [
        // Einnahmen
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "4400", KontoBezeichnung = "Erlöse 19% USt",          KontoTyp = KontoTyp.Einnahme, UstSatz = 19 },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "4300", KontoBezeichnung = "Erlöse 7% USt",           KontoTyp = KontoTyp.Einnahme, UstSatz = 7  },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "4120", KontoBezeichnung = "Steuerfreie Erlöse",      KontoTyp = KontoTyp.Einnahme, UstSatz = 0  },
        // Bank
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "1800", KontoBezeichnung = "Bank",                   KontoTyp = KontoTyp.Bank },
        // Ausgaben
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "5100", KontoBezeichnung = "Wareneingang 19% Vorsteuer",   KontoTyp = KontoTyp.Ausgabe, UstSatz = 19 },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6000", KontoBezeichnung = "Löhne und Gehälter",           KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6010", KontoBezeichnung = "Sozialversicherung AG-Anteil", KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6310", KontoBezeichnung = "Miete",                        KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6325", KontoBezeichnung = "Heizung, Strom, Wasser",       KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6520", KontoBezeichnung = "Fahrzeugkosten",               KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6650", KontoBezeichnung = "Reisekosten",                  KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6600", KontoBezeichnung = "Werbekosten",                  KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6815", KontoBezeichnung = "Bürobedarf",                   KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6820", KontoBezeichnung = "EDV-Kosten, Software",         KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6805", KontoBezeichnung = "Telefon, Internet",            KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6400", KontoBezeichnung = "Versicherungen",               KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6420", KontoBezeichnung = "Beiträge und Gebühren",        KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6300", KontoBezeichnung = "Fremdleistungen",              KontoTyp = KontoTyp.Ausgabe },
        new Konto { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", KontoNummer = "6830", KontoBezeichnung = "Sonstige Kosten",              KontoTyp = KontoTyp.Ausgabe },
    ];

    // ─── KATEGORIE-MAPPINGS ────────────────────────────────────────────────────

    private static async Task SeedMappingsAsync(SaldoDbContext context)
    {
        var existingMappings = await context.KategorieKontoMappings
            .Select(m => new { m.Kontenrahmen, m.ReceiptaKategorie }).ToListAsync();
        var existingSet = existingMappings
            .Select(m => $"{m.Kontenrahmen}:{m.ReceiptaKategorie}").ToHashSet();

        var mappings = GetSKR03Mappings().Concat(GetSKR04Mappings())
            .Where(m => !existingSet.Contains($"{m.Kontenrahmen}:{m.ReceiptaKategorie}"))
            .ToList();

        if (mappings.Count > 0)
        {
            context.KategorieKontoMappings.AddRange(mappings);
            await context.SaveChangesAsync();
        }
    }

    private static IEnumerable<KategorieKontoMapping> GetSKR03Mappings() =>
    [
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Material),       KontoNummer = "3300" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Subcontractor),  KontoNummer = "4900" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Wages),          KontoNummer = "4100" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.SocialSecurity), KontoNummer = "4120" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Rent),          KontoNummer = "4210" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Utilities),     KontoNummer = "4240" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Vehicle),       KontoNummer = "4500" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Travel),        KontoNummer = "4660" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Marketing),     KontoNummer = "4600" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Office),        KontoNummer = "4930" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Software),      KontoNummer = "4964" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Phone),         KontoNummer = "4920" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Insurance),     KontoNummer = "4360" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Fees),          KontoNummer = "4380" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR03", ReceiptaKategorie = nameof(DocumentCategory.Other),         KontoNummer = "4950" },
    ];

    private static IEnumerable<KategorieKontoMapping> GetSKR04Mappings() =>
    [
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Material),       KontoNummer = "5100" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Subcontractor),  KontoNummer = "6300" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Wages),          KontoNummer = "6000" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.SocialSecurity), KontoNummer = "6010" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Rent),          KontoNummer = "6310" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Utilities),     KontoNummer = "6325" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Vehicle),       KontoNummer = "6520" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Travel),        KontoNummer = "6650" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Marketing),     KontoNummer = "6600" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Office),        KontoNummer = "6815" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Software),      KontoNummer = "6820" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Phone),         KontoNummer = "6805" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Insurance),     KontoNummer = "6400" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Fees),          KontoNummer = "6420" },
        new KategorieKontoMapping { Id = Guid.NewGuid(), Kontenrahmen = "SKR04", ReceiptaKategorie = nameof(DocumentCategory.Other),         KontoNummer = "6830" },
    ];
}
