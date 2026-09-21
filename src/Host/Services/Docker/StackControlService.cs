namespace Kuestencode.Werkbank.Host.Services.Docker;

public record StackModule(string Key, string ContainerName, int RequiresCount);

/// <summary>Was vor dem Stoppen lief - nur das wird danach wieder gestartet (manuell gestoppte Module bleiben aus).</summary>
public record StackSnapshot(bool PostgresWasRunning, IReadOnlyList<StackModule> RunningModules);

/// <summary>
/// Stoppt und startet die Werkbank kontrolliert für Vorgänge wie den Restore: erst alle Module,
/// dann Postgres (sauberer Fast-Shutdown); beim Start umgekehrt, mit Warten auf einen gesunden
/// Postgres. Der Host selbst wird nie angefasst - er führt den Vorgang ja aus.
/// </summary>
public interface IStackControlService
{
    /// <exception cref="InvalidOperationException">Postgres-Container fehlt oder Stoppen scheitert; bereits Gestopptes wird vorher wieder gestartet.</exception>
    Task<StackSnapshot> StopAsync(CancellationToken ct = default);

    /// <exception cref="InvalidOperationException">Postgres wird nicht rechtzeitig gesund.</exception>
    Task StartAsync(StackSnapshot snapshot, CancellationToken ct = default);
}

public class StackControlService : IStackControlService
{
    private const int ModuleStopGraceSeconds = 30;
    private const int PostgresStopGraceSeconds = 60;

    private readonly IDockerControlService _docker;
    private readonly IModuleControlService _modules;
    private readonly ILogger<StackControlService> _logger;
    private readonly string _postgresContainer;
    private readonly TimeSpan _postgresStartTimeout;
    private readonly TimeSpan _pollInterval;

    public StackControlService(
        IDockerControlService docker,
        IModuleControlService modules,
        IConfiguration configuration,
        ILogger<StackControlService> logger)
    {
        _docker = docker;
        _modules = modules;
        _logger = logger;

        var pattern = configuration["DockerControl:ContainerNamePattern"] ?? "kuestencode_werkbank_{0}";
        _postgresContainer = string.Format(pattern, "postgres");
        _postgresStartTimeout = TimeSpan.FromSeconds(configuration.GetValue("DockerControl:PostgresStartTimeoutSeconds", 120));
        _pollInterval = TimeSpan.FromMilliseconds(configuration.GetValue("DockerControl:PollIntervalMilliseconds", 2000));
    }

    public async Task<StackSnapshot> StopAsync(CancellationToken ct = default)
    {
        var postgres = await _docker.GetStateAsync(_postgresContainer, ct)
            ?? throw new InvalidOperationException($"Postgres-Container '{_postgresContainer}' nicht gefunden");

        var running = (await _modules.GetStatusAsync(ct))
            .Where(s => s.IsRunning)
            .Select(s => new StackModule(s.Module.Key, s.ContainerName, s.Module.Requires.Count))
            .ToList();

        var snapshot = new StackSnapshot(postgres.IsRunning, running);

        try
        {
            foreach (var module in running)
                await _docker.StopAsync(module.ContainerName, ModuleStopGraceSeconds, ct);

            if (postgres.IsRunning)
                await _docker.StopAsync(_postgresContainer, PostgresStopGraceSeconds, ct);
        }
        catch
        {
            await TryStartAsync(snapshot);
            throw;
        }

        return snapshot;
    }

    public async Task StartAsync(StackSnapshot snapshot, CancellationToken ct = default)
    {
        if (snapshot.PostgresWasRunning)
        {
            await _docker.StartAsync(_postgresContainer, ct);
            await WaitForPostgresHealthyAsync(ct);
        }

        // Module mit Abhängigkeiten zuletzt (z.B. Saldo nach Faktura/Recepta).
        foreach (var module in snapshot.RunningModules.OrderBy(m => m.RequiresCount))
        {
            try
            {
                await _docker.StartAsync(module.ContainerName, ct);
            }
            catch (Exception ex)
            {
                // Postgres läuft - ein einzelnes Modul, das nicht startet, kippt den Vorgang nicht,
                // es lässt sich anschließend über die Modulsteuerung starten.
                _logger.LogWarning(ex, "Modul {Module} konnte nach dem Vorgang nicht gestartet werden", module.Key);
            }
        }
    }

    private async Task WaitForPostgresHealthyAsync(CancellationToken ct)
    {
        var deadline = DateTime.UtcNow + _postgresStartTimeout;
        ContainerState? state;

        while (true)
        {
            state = await _docker.GetStateAsync(_postgresContainer, ct);
            if (state?.IsHealthy == true)
                return;

            if (DateTime.UtcNow >= deadline)
                break;

            await Task.Delay(_pollInterval, ct);
        }

        throw new InvalidOperationException(
            $"Postgres wurde nach {(int)_postgresStartTimeout.TotalSeconds}s nicht gesund (Status: {state?.Status ?? "unbekannt"}, Health: {state?.Health ?? "-"})");
    }

    private async Task TryStartAsync(StackSnapshot snapshot)
    {
        try
        {
            await StartAsync(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Automatischer Neustart nach fehlgeschlagenem Stoppen ebenfalls fehlgeschlagen");
        }
    }
}
