using FluentAssertions;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackInstanceServiceTests
{
    private readonly HostDbContext _context = FeedbackTestData.CreateContext();
    private readonly FeedbackInstanceService _service;

    public FeedbackInstanceServiceTests()
    {
        _service = new FeedbackInstanceService(_context);
    }

    [Fact]
    public async Task CreateInstanceAsync_SpeichertNurHashDesKeys()
    {
        var (instance, apiKey) = await _service.CreateInstanceAsync("  Tischlerei Nord  ");

        apiKey.Should().StartWith("wbfb_");
        instance.Name.Should().Be("Tischlerei Nord");
        var stored = await _context.FeedbackInstances.SingleAsync();
        stored.ApiKeyHash.Should().Be(FeedbackApiKey.Hash(apiKey)).And.NotContain(apiKey);
        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateInstanceAsync_OhneName_WirftFehler()
    {
        var act = () => _service.CreateInstanceAsync(" ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task FindActiveByApiKeyAsync_FindetInstanzNurMitPassendemKey()
    {
        var (instance, apiKey) = await _service.CreateInstanceAsync("Kunde");

        (await _service.FindActiveByApiKeyAsync(apiKey))!.Id.Should().Be(instance.Id);
        (await _service.FindActiveByApiKeyAsync("wbfb_falsch")).Should().BeNull();
    }

    [Fact]
    public async Task SetActiveAsync_DeaktivierteInstanz_WirdNichtMehrGefunden()
    {
        var (instance, apiKey) = await _service.CreateInstanceAsync("Kunde");

        await _service.SetActiveAsync(instance.Id, false);

        (await _service.FindActiveByApiKeyAsync(apiKey)).Should().BeNull();
        (await _service.GetInstancesAsync()).Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RegenerateApiKeyAsync_MachtAltenKeyUngueltig()
    {
        var (instance, oldKey) = await _service.CreateInstanceAsync("Kunde");

        var newKey = await _service.RegenerateApiKeyAsync(instance.Id);

        newKey.Should().NotBe(oldKey);
        (await _service.FindActiveByApiKeyAsync(oldKey)).Should().BeNull();
        (await _service.FindActiveByApiKeyAsync(newKey)).Should().NotBeNull();
    }

    [Fact]
    public async Task RegenerateApiKeyAsync_UnbekannteInstanz_WirftFehler()
    {
        var act = () => _service.RegenerateApiKeyAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Hash_IstDeterministischUndIgnoriertLeerzeichen()
    {
        FeedbackApiKey.Hash(" key ").Should().Be(FeedbackApiKey.Hash("key")).And.HaveLength(64);
    }
}
