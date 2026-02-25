using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class WerkbankSettingsService : IWerkbankSettingsService
{
    private readonly HostDbContext _context;

    public WerkbankSettingsService(HostDbContext context)
    {
        _context = context;
    }

    public async Task<WerkbankSettings> GetSettingsAsync()
    {
        var settings = await _context.WerkbankSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new WerkbankSettings
            {
                Id = Guid.NewGuid(),
                AuthEnabled = false
            };
            _context.WerkbankSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task<WerkbankSettings> UpdateSettingsAsync(WerkbankSettings settings)
    {
        var existing = await _context.WerkbankSettings.FirstOrDefaultAsync();

        if (existing == null)
        {
            _context.WerkbankSettings.Add(settings);
        }
        else
        {
            existing.BaseUrl = settings.BaseUrl;
            existing.AuthEnabled = settings.AuthEnabled;
        }

        await _context.SaveChangesAsync();
        return existing ?? settings;
    }
}
