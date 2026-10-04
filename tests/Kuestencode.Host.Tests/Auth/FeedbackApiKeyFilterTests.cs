using FluentAssertions;
using Kuestencode.Werkbank.Host.Auth;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Hub;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Auth;

public class FeedbackApiKeyFilterTests
{
    private readonly FeedbackModeState _modeState = new();
    private readonly Mock<IFeedbackInstanceService> _instanceService = new();
    private readonly FeedbackApiKeyFilter _filter;

    public FeedbackApiKeyFilterTests()
    {
        _modeState.SetRole(FeedbackRole.Hub);
        _filter = new FeedbackApiKeyFilter(_modeState, _instanceService.Object);
    }

    private static AuthorizationFilterContext CreateContext(string? apiKey)
    {
        var httpContext = new DefaultHttpContext();
        if (apiKey != null)
            httpContext.Request.Headers[FeedbackApiKeyFilter.HeaderName] = apiKey;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public async Task GueltigerKey_LegtInstanzImKontextAbUndLaesstDurch()
    {
        var instance = new FeedbackInstance { Id = 7, Name = "Kunde" };
        _instanceService.Setup(s => s.FindActiveByApiKeyAsync("wbfb_ok")).ReturnsAsync(instance);
        var context = CreateContext("wbfb_ok");

        await _filter.OnAuthorizationAsync(context);

        context.Result.Should().BeNull();
        context.HttpContext.Items[FeedbackApiKeyFilter.InstanceItemKey].Should().BeSameAs(instance);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wbfb_unbekannt")]
    public async Task FehlenderOderUnbekannterKey_Liefert401(string? apiKey)
    {
        var context = CreateContext(apiKey);

        await _filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<UnauthorizedResult>();
        context.HttpContext.Items.Should().NotContainKey(FeedbackApiKeyFilter.InstanceItemKey);
    }

    [Theory]
    [InlineData(FeedbackRole.Aus)]
    [InlineData(FeedbackRole.Client)]
    public async Task OhneHubRolle_Liefert404(FeedbackRole role)
    {
        _modeState.SetRole(role);
        var context = CreateContext("wbfb_ok");

        await _filter.OnAuthorizationAsync(context);

        context.Result.Should().BeOfType<NotFoundResult>();
        _instanceService.Verify(s => s.FindActiveByApiKeyAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Attribut_VerweistAufFilter()
    {
        new FeedbackApiKeyAttribute().ImplementationType.Should().Be(typeof(FeedbackApiKeyFilter));
    }
}
