using Kuestencode.Werkbank.Saldo.Data.Repositories;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Entities;

namespace Kuestencode.Werkbank.Saldo.Services;

public class SaldoSettingsService : ISaldoSettingsService
{
    private readonly ISaldoSettingsRepository _repository;

    public SaldoSettingsService(ISaldoSettingsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SaldoSettingsDto?> GetSettingsAsync()
    {
        var settings = await _repository.GetAsync();
        return settings == null ? null : MapToDto(settings);
    }

    public async Task<SaldoSettingsDto> UpdateSettingsAsync(UpdateSaldoSettingsDto dto)
    {
        var settings = await _repository.GetAsync();

        if (settings == null)
        {
            settings = new SaldoSettings
            {
                Kontenrahmen = dto.Kontenrahmen,
                BeraterNummer = dto.BeraterNummer,
                MandantenNummer = dto.MandantenNummer,
                WirtschaftsjahrBeginn = dto.WirtschaftsjahrBeginn
            };
            settings = await _repository.CreateAsync(settings);
        }
        else
        {
            settings.Kontenrahmen = dto.Kontenrahmen;
            settings.BeraterNummer = dto.BeraterNummer;
            settings.MandantenNummer = dto.MandantenNummer;
            settings.WirtschaftsjahrBeginn = dto.WirtschaftsjahrBeginn;
            settings = await _repository.UpdateAsync(settings);
        }

        return MapToDto(settings);
    }

    private static SaldoSettingsDto MapToDto(SaldoSettings s) => new()
    {
        Id = s.Id,
        Kontenrahmen = s.Kontenrahmen,
        BeraterNummer = s.BeraterNummer,
        MandantenNummer = s.MandantenNummer,
        WirtschaftsjahrBeginn = s.WirtschaftsjahrBeginn
    };
}
