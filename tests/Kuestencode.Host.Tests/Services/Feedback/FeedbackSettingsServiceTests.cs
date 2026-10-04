using FluentAssertions;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackSettingsServiceTests
{
    private readonly HostDbContext _context = FeedbackTestData.CreateContext();
    private readonly FeedbackModeState _modeState = new();
    private readonly FeedbackSettingsService _service;

    public FeedbackSettingsServiceTests()
    {
        _service = new FeedbackSettingsService(
            _context, new PasswordEncryptionService(new EphemeralDataProtectionProvider()), _modeState);
    }

    [Fact]
    public async Task GetSettingsAsync_ErstelltStandardwerte()
    {
        var settings = await _service.GetSettingsAsync();

        settings.Role.Should().Be(FeedbackRole.Aus);
        settings.MaxFileSizeMb.Should().Be(5);
        settings.MaxReportSizeMb.Should().Be(20);
        (await _context.FeedbackSettings.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateSettingsAsync_VerschluesseltKeyUndSetztRolle()
    {
        var settings = await _service.GetSettingsAsync();
        settings.Role = FeedbackRole.Client;
        settings.HubUrl = " https://hub.example.com/ ";

        await _service.UpdateSettingsAsync(settings, "wbfb_geheim");

        var stored = await _context.FeedbackSettings.AsNoTracking().SingleAsync();
        stored.ApiKey.Should().NotBe("wbfb_geheim");
        stored.HubUrl.Should().Be("https://hub.example.com");
        (await _service.GetApiKeyAsync()).Should().Be("wbfb_geheim");
        _modeState.Role.Should().Be(FeedbackRole.Client);
    }

    [Fact]
    public async Task UpdateSettingsAsync_OhneNeuenKey_BehaeltKeyUndSyncStand()
    {
        var settings = await _service.GetSettingsAsync();
        settings.HubUrl = "https://hub.example.com";
        await _service.UpdateSettingsAsync(settings, "wbfb_geheim");
        var syncTime = new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        await _service.SetLastSyncAtAsync(syncTime);

        var reloaded = await _service.GetSettingsAsync();
        reloaded.NotifyOnNewReport = true;
        await _service.UpdateSettingsAsync(reloaded, null);

        (await _service.GetApiKeyAsync()).Should().Be("wbfb_geheim");
        (await _service.GetSettingsAsync()).LastSyncAt.Should().Be(syncTime);
    }

    [Fact]
    public async Task UpdateSettingsAsync_AndererHub_SetztSyncStandZurueck()
    {
        var settings = await _service.GetSettingsAsync();
        settings.HubUrl = "https://hub-a.example.com";
        await _service.UpdateSettingsAsync(settings, null);
        await _service.SetLastSyncAtAsync(DateTime.UtcNow);

        var reloaded = await _service.GetSettingsAsync();
        reloaded.HubUrl = "https://hub-b.example.com";
        await _service.UpdateSettingsAsync(reloaded, null);

        (await _service.GetSettingsAsync()).LastSyncAt.Should().BeNull();
    }

    [Fact]
    public async Task GetApiKeyAsync_OhneKey_LiefertNull()
    {
        (await _service.GetApiKeyAsync()).Should().BeNull();
    }
}
