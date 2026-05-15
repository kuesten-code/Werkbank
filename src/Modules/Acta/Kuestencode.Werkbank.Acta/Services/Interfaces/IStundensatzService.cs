using Kuestencode.Werkbank.Acta.Domain.Dtos;

namespace Kuestencode.Werkbank.Acta.Services;

public interface IStundensatzService
{
    Task<List<StundensatzDto>> GetStundensaetzeAsync(Guid projektId);
    Task UpsertStundensatzAsync(Guid projektId, int rolleId, string rolleName, decimal stundensatz);
    Task<ProjektAbrechnung> GetProjektAbrechnungAsync(Guid projektId);
    Task MarkProjectTimeEntriesAsInvoicedAsync(int externalProjectId);
}
