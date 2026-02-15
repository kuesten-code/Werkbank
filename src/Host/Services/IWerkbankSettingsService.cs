using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public interface IWerkbankSettingsService
{
    Task<WerkbankSettings> GetSettingsAsync();
    Task<WerkbankSettings> UpdateSettingsAsync(WerkbankSettings settings);
}
