using Kuestencode.Shared.Contracts.Locatio;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;
using Kuestencode.Werkbank.Saldo.Domain.Enums;

namespace Kuestencode.Werkbank.Saldo.Services;

/// <summary>
/// Fallback-Konten für Buchungen ohne eigenes Mapping.
/// </summary>
public static class StandardKonten
{
    public static string SonstigeAusgaben(string kontenrahmen) => kontenrahmen == "SKR04" ? "4900" : "4980";

    public static Konto? Erloeskonto(List<Konto> konten, decimal ustSatz) =>
        konten.FirstOrDefault(k => k.KontoTyp == KontoTyp.Einnahme && k.UstSatz == ustSatz);

    /// <summary>
    /// Übernimmt eine Locatio-Buchung. Hat die Locatio-Kategorie kein Konto, greifen dieselben
    /// Fallbacks wie für Faktura (Erlöskonto nach USt-Satz) und Recepta (sonstige Aufwendungen).
    /// </summary>
    public static BuchungDto AusLocatio(LocatioBuchungDto dto, List<Konto> konten, string kontenrahmen)
    {
        var kontoNummer = dto.KontoNummer
            ?? (dto.IstEinnahme ? Erloeskonto(konten, dto.UstSatz)?.KontoNummer ?? "8400" : SonstigeAusgaben(kontenrahmen));
        var konto = konten.FirstOrDefault(k => k.KontoNummer == kontoNummer);

        return new BuchungDto
        {
            Id = dto.Id,
            Quelle = "Locatio",
            QuelleId = dto.Referenz,
            BelegDatum = dto.Zahlungsdatum,
            ZahlungsDatum = dto.Zahlungsdatum,
            Beschreibung = dto.Beschreibung,
            Netto = dto.Netto,
            Ust = dto.Ust,
            Brutto = dto.Brutto,
            UstSatz = dto.UstSatz,
            Kategorie = dto.Kategorie,
            KontoNummer = kontoNummer,
            KontoBezeichnung = konto?.KontoBezeichnung ?? dto.Kategorie,
            Typ = dto.IstEinnahme ? BuchungsTyp.Einnahme : BuchungsTyp.Ausgabe
        };
    }
}
