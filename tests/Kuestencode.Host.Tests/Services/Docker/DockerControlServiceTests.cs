using System.Net;
using System.Text;
using FluentAssertions;
using Kuestencode.Werkbank.Host.Services.Docker;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Docker;

public class DockerControlServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(HttpStatusCode status, string body = "")
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static (DockerControlService Service, StubHandler Handler) Create(HttpStatusCode status, string body = "")
    {
        var handler = new StubHandler(status, body);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://docker-control:2375") };
        return (new DockerControlService(client), handler);
    }

    [Fact]
    public async Task GetStateAsync_LiestStatusUndHealthAusDerInspectAntwort()
    {
        var (service, handler) = Create(HttpStatusCode.OK,
            """{"State":{"Status":"running","Health":{"Status":"healthy"}}}""");

        var state = await service.GetStateAsync("kuestencode_werkbank_postgres");

        state.Should().Be(new ContainerState("running", "healthy"));
        state!.IsHealthy.Should().BeTrue();
        handler.LastRequest!.RequestUri!.PathAndQuery.Should().Be("/containers/kuestencode_werkbank_postgres/json");
    }

    [Fact]
    public async Task GetStateAsync_OhneHealthcheck_HatKeinenHealthWertUndGiltAlsGesundWennLaufend()
    {
        var (service, _) = Create(HttpStatusCode.OK, """{"State":{"Status":"running"}}""");

        var state = await service.GetStateAsync("faktura");

        state!.Health.Should().BeNull();
        state.IsHealthy.Should().BeTrue();
    }

    [Fact]
    public async Task GetStateAsync_GestoppterContainer_IstNichtGesund()
    {
        var (service, _) = Create(HttpStatusCode.OK,
            """{"State":{"Status":"exited","Health":{"Status":"unhealthy"}}}""");

        var state = await service.GetStateAsync("postgres");

        state!.IsRunning.Should().BeFalse();
        state.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task GetStateAsync_UnbekannterContainer_GibtNullZurueck()
    {
        var (service, _) = Create(HttpStatusCode.NotFound);

        (await service.GetStateAsync("gibt-es-nicht")).Should().BeNull();
    }

    [Fact]
    public async Task GetStateAsync_ProxyVerweigertZugriff_WirftVerstaendlicheException()
    {
        var (service, _) = Create(HttpStatusCode.Forbidden);

        var act = async () => await service.GetStateAsync("host");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht erlaubt*");
    }

    [Fact]
    public async Task StopAsync_SendetPostMitGnadenfrist()
    {
        var (service, handler) = Create(HttpStatusCode.NoContent);

        await service.StopAsync("postgres", gracePeriodSeconds: 45);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/containers/postgres/stop?t=45");
    }

    [Fact]
    public async Task StartAsync_SendetPostAnStartEndpunkt()
    {
        var (service, handler) = Create(HttpStatusCode.NoContent);

        await service.StartAsync("postgres");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/containers/postgres/start");
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("start")]
    public async Task StopUndStart_ContainerWarSchonImZielzustand_WirdAlsErfolgGewertet(string action)
    {
        var (service, _) = Create(HttpStatusCode.NotModified);

        var act = async () => await (action == "stop" ? service.StopAsync("postgres") : service.StartAsync("postgres"));

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_UnbekannterContainer_WirftException()
    {
        var (service, _) = Create(HttpStatusCode.NotFound);

        var act = async () => await service.StopAsync("gibt-es-nicht");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nicht gefunden*");
    }

    [Fact]
    public async Task StartAsync_ServerFehler_WirftExceptionMitStatuscode()
    {
        var (service, _) = Create(HttpStatusCode.InternalServerError, "boom");

        var act = async () => await service.StartAsync("postgres");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*HTTP 500*boom*");
    }

    [Fact]
    public async Task Containername_WirdFuerDenPfadUrlKodiert()
    {
        var (service, handler) = Create(HttpStatusCode.NoContent);

        await service.StartAsync("evil/../host");

        handler.LastRequest!.RequestUri!.AbsolutePath.Should().Be("/containers/evil%2F..%2Fhost/start");
    }
}
