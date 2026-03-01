using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

public interface IEuerService
{
    Task<EuerSummaryDto> GetEuerSummaryAsync(EuerFilterDto filter);
}
