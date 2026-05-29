using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Faktura;
using Kuestencode.Werkbank.Contracta.Data.Repositories;
using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Kuestencode.Werkbank.Contracta.Domain.Enums;
using Kuestencode.Werkbank.Contracta.Domain.Interfaces;
using Kuestencode.Werkbank.Contracta.Services.Interfaces;

namespace Kuestencode.Werkbank.Contracta.Services.Implementation;

public class RechnungserstellungService : IRechnungserstellungService
{
    private readonly IFakturaApiClient _fakturaApiClient;
    private readonly IModulVerfuegbarkeit _modulVerfuegbarkeit;
    private readonly IWartungsvertragRepository _repository;
    private readonly IFaelligkeitsService _faelligkeitsService;

    public RechnungserstellungService(
        IFakturaApiClient fakturaApiClient,
        IModulVerfuegbarkeit modulVerfuegbarkeit,
        IWartungsvertragRepository repository,
        IFaelligkeitsService faelligkeitsService)
    {
        _fakturaApiClient = fakturaApiClient;
        _modulVerfuegbarkeit = modulVerfuegbarkeit;
        _repository = repository;
        _faelligkeitsService = faelligkeitsService;
    }

    public bool IstVerfuegbar => _modulVerfuegbarkeit.IstFakturaVerfuegbar;

    public async Task<RechnungserstellungErgebnis> ErstelleRechnungAsync(Guid vertragId)
    {
        if (!IstVerfuegbar)
            return Fehlschlag(vertragId, "Faktura-Modul nicht verfügbar.");

        var vertrag = await _repository.GetByIdAsync(vertragId);
        if (vertrag == null)
            return Fehlschlag(vertragId, "Wartungsvertrag nicht gefunden.");
        if (vertrag.Status != VertragStatus.Aktiv)
            return Fehlschlag(vertragId, $"Nur aktive Verträge können abgerechnet werden (aktueller Status: {vertrag.Status}).");

        try
        {
            var request = MapToRequest(vertrag);
            var rechnung = await _fakturaApiClient.CreateInvoiceAsync(request);

            vertrag.LetzteAbrechnung = DateTime.UtcNow;
            vertrag.NaechsteAbrechnung = _faelligkeitsService.BerechneNaechsteFaelligkeit(vertrag);
            vertrag.Historien.Add(new Abrechnungshistorie
            {
                Id = Guid.NewGuid(),
                WartungsvertragId = vertrag.Id,
                Abrechnungsdatum = DateTime.UtcNow,
                RechnungId = rechnung.Id,
                Rechnungsnummer = rechnung.InvoiceNumber,
                Betrag = rechnung.TotalGross
            });
            await _repository.UpdateAsync(vertrag);

            return new RechnungserstellungErgebnis
            {
                VertragId = vertragId,
                Erfolgreich = true,
                RechnungId = rechnung.Id,
                Rechnungsnummer = rechnung.InvoiceNumber
            };
        }
        catch (Exception ex)
        {
            return Fehlschlag(vertragId, ex.Message);
        }
    }

    public async Task<List<RechnungserstellungErgebnis>> ErstelleRechnungenAsync(List<Guid> vertragIds)
    {
        var ergebnisse = new List<RechnungserstellungErgebnis>();
        foreach (var id in vertragIds)
            ergebnisse.Add(await ErstelleRechnungAsync(id));
        return ergebnisse;
    }

    private static CreateInvoiceRequest MapToRequest(Wartungsvertrag vertrag) =>
        new()
        {
            InvoiceDate = DateTime.UtcNow,
            CustomerId = vertrag.KundeId,
            Notes = string.IsNullOrWhiteSpace(vertrag.Notizen)
                ? $"Vertrag {vertrag.Vertragsnummer}"
                : $"Vertrag {vertrag.Vertragsnummer} – {vertrag.Notizen}",
            Items = vertrag.Positionen.Select(p => new CreateInvoiceItemRequest
            {
                Description = p.Text,
                Quantity = p.Menge,
                UnitPrice = p.Einzelpreis,
                VatRate = p.Steuersatz
            }).ToList()
        };

    private static RechnungserstellungErgebnis Fehlschlag(Guid vertragId, string fehler) =>
        new() { VertragId = vertragId, Erfolgreich = false, Fehler = fehler };
}
