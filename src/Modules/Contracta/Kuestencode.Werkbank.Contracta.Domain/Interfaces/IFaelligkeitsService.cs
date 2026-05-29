using Kuestencode.Werkbank.Contracta.Domain.Entities;

namespace Kuestencode.Werkbank.Contracta.Domain.Interfaces;

public interface IFaelligkeitsService
{
    DateTime BerechneNaechsteFaelligkeit(Wartungsvertrag vertrag);
    bool IstFaellig(Wartungsvertrag vertrag, DateTime stichtag);
    Task<List<Wartungsvertrag>> GetFaelligeVertraegeAsync(DateTime stichtag);
}
