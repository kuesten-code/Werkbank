using FluentAssertions;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services;

public class HostNavigationServiceTests
{
    private readonly FeedbackModeState _feedbackMode = new();
    private readonly HostNavigationService _service;

    public HostNavigationServiceTests()
    {
        var registry = new Mock<IModuleRegistry>();
        registry.Setup(r => r.GetAllModules()).Returns(new List<ModuleInfoDto>());
        _service = new HostNavigationService(registry.Object, _feedbackMode);
    }

    private static IEnumerable<NavItemDto> Flatten(IEnumerable<NavItemDto> items) =>
        items.SelectMany(i => new[] { i }.Concat(Flatten(i.Children)));

    [Fact]
    public void RolleAus_ZeigtNurEinstellungsseite()
    {
        var hrefs = Flatten(_service.GetNavigationItems()).Select(i => i.Href).ToList();

        hrefs.Should().Contain("/settings/feedback");
        hrefs.Should().NotContain(new[] { "/feedback/new", "/feedback", "/feedback/hub" });
    }

    [Fact]
    public void RolleClient_ZeigtMeldenUndMeineMeldungenFuerAlleRollen()
    {
        _feedbackMode.SetRole(FeedbackRole.Client);

        var items = _service.GetVisibleNavigationItems(UserRole.Mitarbeiter, authEnabled: true);

        Flatten(items).Select(i => i.Href).Should().Contain(new[] { "/feedback/new", "/feedback" }).And.NotContain("/feedback/hub");
    }

    [Fact]
    public void RolleHub_ZeigtHubNurFuerAdmin()
    {
        _feedbackMode.SetRole(FeedbackRole.Hub);

        Flatten(_service.GetVisibleNavigationItems(UserRole.Admin, true)).Select(i => i.Href).Should().Contain("/feedback/hub");
        Flatten(_service.GetVisibleNavigationItems(UserRole.Buero, true)).Select(i => i.Href).Should().NotContain("/feedback/hub");
    }
}
