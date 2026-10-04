using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services.Feedback;

public class FeedbackSettingsService : IFeedbackSettingsService
{
    private readonly HostDbContext _context;
    private readonly PasswordEncryptionService _encryption;
    private readonly IFeedbackModeState _modeState;

    public FeedbackSettingsService(HostDbContext context, PasswordEncryptionService encryption, IFeedbackModeState modeState)
    {
        _context = context;
        _encryption = encryption;
        _modeState = modeState;
    }

    // AsNoTracking/Detach wie bei BackupService: HostDbContext lebt in Blazor Server pro
    // Circuit, die an die UI gegebene Instanz darf nicht die getrackte sein.
    public async Task<FeedbackSettings> GetSettingsAsync()
    {
        var settings = await _context.FeedbackSettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings != null)
            return settings;

        settings = new FeedbackSettings();
        _context.FeedbackSettings.Add(settings);
        await _context.SaveChangesAsync();
        _context.Entry(settings).State = EntityState.Detached;
        return settings;
    }

    public async Task UpdateSettingsAsync(FeedbackSettings settings, string? newApiKey)
    {
        var existing = await GetTrackedSettingsAsync();
        var hubUrl = settings.HubUrl?.Trim().TrimEnd('/');
        var hubChanged = existing.HubUrl != hubUrl || !string.IsNullOrWhiteSpace(newApiKey);

        existing.Role = settings.Role;
        existing.HubUrl = hubUrl;
        existing.NotifyOnNewReport = settings.NotifyOnNewReport;
        existing.NotificationEmail = settings.NotificationEmail?.Trim();
        existing.MaxFileSizeMb = settings.MaxFileSizeMb;
        existing.MaxReportSizeMb = settings.MaxReportSizeMb;

        if (!string.IsNullOrWhiteSpace(newApiKey))
            existing.ApiKey = _encryption.Encrypt(newApiKey.Trim());

        // Anderer Hub oder anderer Key = andere Instanz: Polling muss von vorn beginnen.
        if (hubChanged)
            existing.LastSyncAt = null;

        await _context.SaveChangesAsync();
        _modeState.SetRole(existing.Role);
    }

    public async Task<string?> GetApiKeyAsync()
    {
        var settings = await GetSettingsAsync();
        return string.IsNullOrEmpty(settings.ApiKey) ? null : _encryption.Decrypt(settings.ApiKey);
    }

    public async Task SetLastSyncAtAsync(DateTime serverTime)
    {
        var existing = await GetTrackedSettingsAsync();
        existing.LastSyncAt = serverTime;
        await _context.SaveChangesAsync();
    }

    private async Task<FeedbackSettings> GetTrackedSettingsAsync()
    {
        await GetSettingsAsync();
        return await _context.FeedbackSettings.FirstAsync();
    }
}
