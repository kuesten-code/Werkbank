using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Models;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Services;

namespace Kuestencode.Faktura.Services.Positionskatalog;

public static class FakturaPositionskatalog
{
    /// <summary>
    /// Positionen einer Rechnung für den Katalog. Gutschriften tragen nichts bei – ihre Preise sind negiert.
    /// Stundenpositionen filtert der Host.
    /// </summary>
    public static List<PositionsvorlageMerkenDto> AusRechnung(Invoice invoice)
    {
        if (invoice.Type == InvoiceType.CreditNote)
            return [];

        return invoice.Items
            .Where(i => !i.IsHeader)
            .Select(i => new PositionsvorlageMerkenDto
            {
                Text = i.Description,
                Einheit = i.Unit,
                Einzelpreis = i.UnitPrice,
                Dokumentdatum = invoice.InvoiceDate
            })
            .ToList();
    }

    public static void Uebernehmen(InvoiceItem item, PositionsvorlageDto vorlage)
    {
        item.Unit = vorlage.Einheit;
        item.UnitPrice = vorlage.Einzelpreis;
    }
}

public class FakturaPositionskatalogQuelle : IPositionskatalogQuelle
{
    private readonly IInvoiceRepository _invoiceRepository;

    public FakturaPositionskatalogQuelle(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<List<PositionsvorlageMerkenDto>> GetPositionenAsync()
    {
        var rechnungen = await _invoiceRepository.GetRechnungenMitPositionenAsync();
        return rechnungen.SelectMany(FakturaPositionskatalog.AusRechnung).ToList();
    }
}
