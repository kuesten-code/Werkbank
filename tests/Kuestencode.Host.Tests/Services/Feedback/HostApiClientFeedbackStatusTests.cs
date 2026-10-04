using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Feedback;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class HostApiClientFeedbackStatusTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private static HostApiClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new StubHandler(respond)) { BaseAddress = new Uri("http://host:8080") });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsFeedbackReportingEnabledAsync_LiefertStatusVomHost(bool enabled)
    {
        Uri? requested = null;
        var client = CreateClient(request =>
        {
            requested = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new FeedbackStatusDto { ReportingEnabled = enabled }) };
        });

        (await client.IsFeedbackReportingEnabledAsync()).Should().Be(enabled);
        requested!.AbsolutePath.Should().Be("/api/feedback/status");
    }

    [Fact]
    public async Task IsFeedbackReportingEnabledAsync_HostFehler_LiefertFalse()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        (await client.IsFeedbackReportingEnabledAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task IsFeedbackReportingEnabledAsync_HostNichtErreichbar_LiefertFalse()
    {
        var client = CreateClient(_ => throw new HttpRequestException("Connection refused"));

        (await client.IsFeedbackReportingEnabledAsync()).Should().BeFalse();
    }
}
