using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Moq;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackHubClientTests
{
    private readonly FakeHandler _handler = new();
    private readonly FeedbackHubClient _client;
    private readonly FeedbackHubConnection _connection = new("https://hub.example.com/", "wbfb_key");

    public FeedbackHubClientTests()
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(FeedbackHubClient.HttpClientName)).Returns(() => new HttpClient(_handler));
        _client = new FeedbackHubClient(factory.Object);
    }

    private sealed class FakeHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }
        public Func<HttpResponseMessage> Respond { get; set; } = () => new HttpResponseMessage(HttpStatusCode.OK);
        public Exception? Throw { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            if (Throw != null)
                throw Throw;
            return Respond();
        }
    }

    private static HttpResponseMessage Json<T>(T value, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(value) };

    [Fact]
    public async Task GetInstanceAsync_SendetApiKeyHeaderAnRichtigeUrl()
    {
        _handler.Respond = () => Json(new FeedbackInstanceInfoDto { Name = "Kunde A" });

        var info = await _client.GetInstanceAsync(_connection);

        info.Name.Should().Be("Kunde A");
        _handler.Request!.RequestUri.Should().Be(new Uri("https://hub.example.com/api/v1/instance"));
        _handler.Request.Headers.GetValues("X-Api-Key").Should().Equal("wbfb_key");
    }

    [Fact]
    public async Task CreateReportAsync_SendetMultipartMitJsonUndDateien()
    {
        _handler.Respond = () => Json(new FeedbackReportDto { Id = 5 }, HttpStatusCode.Created);
        var request = new CreateFeedbackReportRequest { ClientReportId = Guid.NewGuid(), Title = "Titel" };

        var dto = await _client.CreateReportAsync(_connection, request, new[] { ("a.png", "image/png", new byte[] { 1 }) });

        dto.Id.Should().Be(5);
        _handler.Request!.Method.Should().Be(HttpMethod.Post);
        _handler.Request.Content.Should().BeOfType<MultipartFormDataContent>();
        _handler.Body.Should().Contain("name=report").And.Contain("\"title\":\"Titel\"").And.Contain("filename=a.png");
    }

    [Fact]
    public async Task GetChangesAsync_UebergibtSinceAlsIsoUtc()
    {
        _handler.Respond = () => Json(new FeedbackSyncResponse());
        var since = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);

        await _client.GetChangesAsync(_connection, since);

        _handler.Request!.RequestUri!.Query.Should().Be("?since=2026-10-03T10%3A00%3A00.0000000Z");
    }

    [Fact]
    public async Task GetChangesAsync_OhneSince_FragtAlleAb()
    {
        _handler.Respond = () => Json(new FeedbackSyncResponse());

        await _client.GetChangesAsync(_connection, null);

        _handler.Request!.RequestUri!.Query.Should().BeEmpty();
    }

    [Fact]
    public async Task AddCommentAsync_PostetJsonAnMeldung()
    {
        _handler.Respond = () => Json(new FeedbackCommentDto { Id = 9 });

        var dto = await _client.AddCommentAsync(_connection, 42, new CreateFeedbackCommentRequest { Text = "Hallo" });

        dto.Id.Should().Be(9);
        _handler.Request!.RequestUri!.AbsolutePath.Should().Be("/api/v1/reports/42/comments");
        _handler.Body.Should().Contain("\"text\":\"Hallo\"");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, true)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, true)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.InternalServerError, false)]
    public async Task Fehlerstatus_WirdAlsHubExceptionMitPassenderPermanenzGeworfen(HttpStatusCode status, bool permanent)
    {
        _handler.Respond = () => new HttpResponseMessage(status) { Content = new StringContent("{\"errors\":[\"x\"]}") };

        var act = () => _client.GetInstanceAsync(_connection);

        (await act.Should().ThrowAsync<FeedbackHubException>()).Which.IsPermanent.Should().Be(permanent);
    }

    [Fact]
    public async Task Netzwerkfehler_WirdAlsVoruebergehenderFehlerGeworfen()
    {
        _handler.Throw = new HttpRequestException("Connection refused");

        var act = () => _client.GetInstanceAsync(_connection);

        (await act.Should().ThrowAsync<FeedbackHubException>())
            .Which.Should().Match<FeedbackHubException>(e => !e.IsPermanent && e.Message.Contains("nicht erreichbar"));
    }

    [Theory]
    [InlineData("kein-url")]
    [InlineData("ftp://hub.example.com")]
    public async Task UngueltigeHubUrl_WirftOhneRequest(string url)
    {
        var act = () => _client.GetInstanceAsync(new FeedbackHubConnection(url, "k"));

        await act.Should().ThrowAsync<FeedbackHubException>();
        _handler.Request.Should().BeNull();
    }
}
