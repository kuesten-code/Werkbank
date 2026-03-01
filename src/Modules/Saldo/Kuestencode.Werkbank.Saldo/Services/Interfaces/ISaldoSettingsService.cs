using Kuestencode.Werkbank.Saldo.Domain.Dtos;

namespace Kuestencode.Werkbank.Saldo.Services;

public interface ISaldoSettingsService
{
    Task<SaldoSettingsDto?> GetSettingsAsync();
    Task<SaldoSettingsDto> UpdateSettingsAsync(UpdateSaldoSettingsDto dto);
}
