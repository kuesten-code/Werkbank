using FluentAssertions;
using Kuestencode.Rapport.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Rapport.IntegrationTests;

public class SettingsTests : IClassFixture<RapportWebApplicationFactory>
{
    private readonly RapportWebApplicationFactory _factory;

    public SettingsTests(RapportWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Settings_Update_ShouldPersist()
    {
        using var scope = _factory.Services.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();

        var settings = await settingsService.GetSettingsAsync();
        settings.DefaultHourlyRate = 120m;
        settings.ShowHourlyRateInPdf = true;
        settings.CalculateTotalAmount = true;

        await settingsService.UpdateSettingsAsync(settings);

        var reloaded = await settingsService.GetSettingsAsync();
        reloaded.DefaultHourlyRate.Should().Be(120m);
        reloaded.ShowHourlyRateInPdf.Should().BeTrue();
        reloaded.CalculateTotalAmount.Should().BeTrue();
    }
}
