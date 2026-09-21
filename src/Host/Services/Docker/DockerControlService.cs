using System.Net;
using System.Text.Json;

namespace Kuestencode.Werkbank.Host.Services.Docker;

public class DockerControlService : IDockerControlService
{
    private readonly HttpClient _httpClient;

    public DockerControlService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ContainerState?> GetStateAsync(string container, CancellationToken ct = default)
    {
        using var response = await _httpClient.GetAsync(ContainerPath(container, "json"), ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, container, ct);

        await using var body = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(body, cancellationToken: ct);
        var state = doc.RootElement.GetProperty("State");

        var status = state.GetProperty("Status").GetString() ?? "unknown";
        string? health = null;
        if (state.TryGetProperty("Health", out var healthElement))
            health = healthElement.GetProperty("Status").GetString();

        return new ContainerState(status, health);
    }

    public Task StopAsync(string container, int gracePeriodSeconds = 30, CancellationToken ct = default) =>
        PostActionAsync(container, $"stop?t={gracePeriodSeconds}", ct);

    public Task StartAsync(string container, CancellationToken ct = default) =>
        PostActionAsync(container, "start", ct);

    private async Task PostActionAsync(string container, string action, CancellationToken ct)
    {
        using var response = await _httpClient.PostAsync(ContainerPath(container, action), content: null, ct);

        // 304 = Container war bereits im gewünschten Zustand.
        if (response.StatusCode == HttpStatusCode.NotModified)
            return;

        await EnsureSuccessAsync(response, container, ct);
    }

    // Bewusst ohne "/v1.xx"-Präfix: Docker nimmt dann die jeweils neueste API-Version, wir sind
    // also unabhängig von der Docker-Version auf dem Server. Die Proxy-Allowlist erlaubt beides.
    private static string ContainerPath(string container, string suffix) =>
        $"/containers/{Uri.EscapeDataString(container)}/{suffix}";

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string container, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var detail = response.StatusCode switch
        {
            HttpStatusCode.Forbidden => "vom Docker-Proxy nicht erlaubt (Container nicht freigegeben)",
            HttpStatusCode.NotFound => "Container nicht gefunden",
            _ => $"HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(ct)}"
        };

        throw new InvalidOperationException($"Docker-Steuerung für '{container}' fehlgeschlagen: {detail}");
    }
}
