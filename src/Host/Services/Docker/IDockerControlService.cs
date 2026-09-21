namespace Kuestencode.Werkbank.Host.Services.Docker;

/// <param name="Status">Docker-Status, z.B. "running", "exited", "restarting".</param>
/// <param name="Health">Ergebnis des Container-Healthchecks ("healthy", "unhealthy", "starting") oder null, wenn der Container keinen definiert.</param>
public record ContainerState(string Status, string? Health)
{
    public bool IsRunning => Status == "running";
    public bool IsHealthy => IsRunning && Health is null or "healthy";
}

/// <summary>
/// Steuert ausgewählte Container über den Docker-Socket-Proxy (wollomatic/socket-proxy), der nur
/// Status-Abfragen sowie stop/start/restart für explizit freigegebene Container durchlässt.
/// Der Host hat selbst keinen Zugriff auf den Docker-Socket.
/// </summary>
public interface IDockerControlService
{
    Task<ContainerState?> GetStateAsync(string container, CancellationToken ct = default);
    Task StopAsync(string container, int gracePeriodSeconds = 30, CancellationToken ct = default);
    Task StartAsync(string container, CancellationToken ct = default);
}
