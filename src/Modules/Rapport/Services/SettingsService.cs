using System.ComponentModel.DataAnnotations;
using Kuestencode.Rapport.Data;
using Kuestencode.Rapport.Models;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Persistent settings storage for Rapport.
/// </summary>
public class SettingsService
{
    private static readonly int[] AllowedRoundingMinutes = { 0, 5, 15, 30 };

    private readonly RapportDbContext _context;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(RapportDbContext context, ILogger<SettingsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RapportSettings> GetSettingsAsync()
    {
        var settings = await _context.Settings.FirstOrDefaultAsync();
        if (settings != null)
        {
            return settings;
        }

        settings = RapportSettings.CreateDefault();
        _context.Settings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task UpdateSettingsAsync(RapportSettings settings)
    {
        Validate(settings);

        var existing = await _context.Settings.FirstOrDefaultAsync();
        if (existing == null)
        {
            settings.Id = 1;
            _context.Settings.Add(settings);
        }
        else
        {
            Apply(existing, settings);
        }

        await _context.SaveChangesAsync();
    }

    public async Task ResetToDefaultAsync()
    {
        var defaults = RapportSettings.CreateDefault();
        var existing = await _context.Settings.FirstOrDefaultAsync();
        if (existing == null)
        {
            _context.Settings.Add(defaults);
        }
        else
        {
            Apply(existing, defaults);
        }

        await _context.SaveChangesAsync();
    }

    private void Validate(RapportSettings settings)
    {
        if (!AllowedRoundingMinutes.Contains(settings.RoundingMinutes))
        {
            throw new ValidationException("RoundingMinutes must be 0, 5, 15 or 30.");
        }

        if (settings.CalculateTotalAmount && !settings.ShowHourlyRateInPdf)
        {
            _logger.LogInformation("CalculateTotalAmount requires ShowHourlyRateInPdf. Enabling ShowHourlyRateInPdf.");
            settings.ShowHourlyRateInPdf = true;
        }

        if ((settings.ShowHourlyRateInPdf || settings.CalculateTotalAmount) && settings.DefaultHourlyRate <= 0)
        {
            throw new ValidationException("DefaultHourlyRate must be greater than 0 when hourly rate is shown or amounts are calculated.");
        }

        if (settings.AutoStopTimerAfterHours.HasValue && settings.AutoStopTimerAfterHours.Value <= 0)
        {
            throw new ValidationException("AutoStopTimerAfterHours must be greater than 0.");
        }

        if (!IsColor(settings.PdfPrimaryColor) || !IsColor(settings.PdfAccentColor))
        {
            throw new ValidationException("PDF colors must be valid hex values like #1f3a5f.");
        }
    }

    private static void Apply(RapportSettings target, RapportSettings source)
    {
        target.DefaultHourlyRate = source.DefaultHourlyRate;
        target.ShowHourlyRateInPdf = source.ShowHourlyRateInPdf;
        target.CalculateTotalAmount = source.CalculateTotalAmount;
        target.RoundingMinutes = source.RoundingMinutes;
        target.StartOfWeek = source.StartOfWeek;
        target.DefaultProjectId = source.DefaultProjectId;
        target.AutoStopTimerAfterHours = source.AutoStopTimerAfterHours;
        target.EnableSounds = source.EnableSounds;
        target.PdfLayout = source.PdfLayout;
        target.PdfPrimaryColor = source.PdfPrimaryColor;
        target.PdfAccentColor = source.PdfAccentColor;
        target.PdfHeaderText = source.PdfHeaderText;
        target.PdfFooterText = source.PdfFooterText;
    }

    private static bool IsColor(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!value.StartsWith("#") || (value.Length != 7 && value.Length != 4))
        {
            return false;
        }

        return value.Skip(1).All(c => Uri.IsHexDigit(c));
    }
}
