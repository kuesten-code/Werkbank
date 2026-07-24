using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Host.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

public class NumberFormatSettingsService : INumberFormatSettingsService
{
    private readonly HostDbContext _context;

    public NumberFormatSettingsService(HostDbContext context)
    {
        _context = context;
    }

    public async Task<NumberFormatSettings> GetSettingsAsync()
    {
        var settings = await _context.NumberFormatSettings.FirstOrDefaultAsync();

        if (settings == null)
        {
            settings = new NumberFormatSettings();
            _context.NumberFormatSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return settings;
    }

    public async Task<NumberFormatSettings> UpdateSettingsAsync(NumberFormatSettings settings)
    {
        var existing = await _context.NumberFormatSettings.FirstOrDefaultAsync();

        if (existing == null)
        {
            _context.NumberFormatSettings.Add(settings);
            existing = settings;
        }
        else
        {
            existing.InvoiceFormat = settings.InvoiceFormat;
            existing.QuoteFormat = settings.QuoteFormat;
            existing.ProjectFormat = settings.ProjectFormat;
            existing.IncomingInvoiceFormat = settings.IncomingInvoiceFormat;
            existing.CreditNoteFormat = settings.CreditNoteFormat;
        }

        await _context.SaveChangesAsync();
        return existing;
    }
}
