using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.UI.Services;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Entities;

namespace Kuestencode.Werkbank.Offerte.Services;

public class AngebotPositionskatalogService : IAngebotPositionskatalogService
{
    private readonly IPositionskatalogLerner _lerner;

    public AngebotPositionskatalogService(IPositionskatalogLerner lerner)
    {
        _lerner = lerner;
    }

    public Task MerkenAsync(Angebot angebot) => _lerner.MerkenAsync(AusAngebot(angebot));

    /// <summary>
    /// Stundenpositionen filtert der Host; Rabatt und Steuersatz gehören zum einzelnen Angebot.
    /// </summary>
    public static List<PositionsvorlageMerkenDto> AusAngebot(Angebot angebot)
        => angebot.Positionen
            .Where(p => !p.IsHeader)
            .Select(p => new PositionsvorlageMerkenDto
            {
                Text = p.Text,
                Einheit = p.Einheit,
                Einzelpreis = p.Einzelpreis,
                Dokumentdatum = angebot.Erstelldatum
            })
            .ToList();

    public static void Uebernehmen(Angebotsposition position, PositionsvorlageDto vorlage)
    {
        position.Einheit = vorlage.Einheit;
        position.Einzelpreis = vorlage.Einzelpreis;
    }
}

public class OffertePositionskatalogQuelle : IPositionskatalogQuelle
{
    private readonly IAngebotRepository _repository;

    public OffertePositionskatalogQuelle(IAngebotRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PositionsvorlageMerkenDto>> GetPositionenAsync()
    {
        var angebote = await _repository.GetAllAsync();
        return angebote.SelectMany(AngebotPositionskatalogService.AusAngebot).ToList();
    }
}
