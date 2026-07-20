using Kuestencode.Core.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface INumberFormatSettingsService
{
    Task<NumberFormatSettings> GetSettingsAsync();
    Task<NumberFormatSettings> UpdateSettingsAsync(NumberFormatSettings settings);
}
