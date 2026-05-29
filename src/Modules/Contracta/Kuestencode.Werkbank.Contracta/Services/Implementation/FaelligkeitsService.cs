using Kuestencode.Werkbank.Contracta.Data.Repositories;
using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Kuestencode.Werkbank.Contracta.Domain.Enums;
using Kuestencode.Werkbank.Contracta.Domain.Interfaces;

namespace Kuestencode.Werkbank.Contracta.Services.Implementation;

public class FaelligkeitsService : IFaelligkeitsService
{
    private readonly IWartungsvertragRepository _repository;

    public FaelligkeitsService(IWartungsvertragRepository repository)
    {
        _repository = repository;
    }

    public DateTime BerechneNaechsteFaelligkeit(Wartungsvertrag vertrag)
    {
        var basis = vertrag.LetzteAbrechnung ?? vertrag.Startdatum;
        return AddIntervall(basis, vertrag.Intervall, vertrag.CustomIntervallTage);
    }

    public bool IstFaellig(Wartungsvertrag vertrag, DateTime stichtag) =>
        vertrag.Status == VertragStatus.Aktiv &&
        vertrag.NaechsteAbrechnung.HasValue &&
        vertrag.NaechsteAbrechnung.Value <= stichtag &&
        (vertrag.Enddatum == null || vertrag.Enddatum.Value >= stichtag);

    public async Task<List<Wartungsvertrag>> GetFaelligeVertraegeAsync(DateTime stichtag)
    {
        var aktive = await _repository.GetAktiveAsync();
        return aktive.Where(v => IstFaellig(v, stichtag)).ToList();
    }

    private static DateTime AddIntervall(DateTime datum, Abrechnungsintervall intervall, int? customTage) =>
        intervall switch
        {
            Abrechnungsintervall.Monatlich => datum.AddMonths(1),
            Abrechnungsintervall.Quartalsweise => datum.AddMonths(3),
            Abrechnungsintervall.Halbjaehrlich => datum.AddMonths(6),
            Abrechnungsintervall.Jaehrlich => datum.AddYears(1),
            Abrechnungsintervall.Custom => datum.AddDays(customTage ?? 30),
            _ => datum.AddMonths(1)
        };
}
