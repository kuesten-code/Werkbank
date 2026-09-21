namespace Kuestencode.Werkbank.Host.Services.Docker;

/// <param name="Requires">Schlüssel der Module, ohne die dieses Modul fachlich nicht sinnvoll läuft (harte Abhängigkeit).</param>
/// <param name="StopHint">Hinweis beim Stoppen bei nur loser Kopplung.</param>
public record ManagedModule(string Key, string DisplayName, IReadOnlyList<string> Requires, string? StopHint);

public record ModuleStatus(ManagedModule Module, string ContainerName, ContainerState State)
{
    public bool IsRunning => State.IsRunning;
}

public interface IModuleControlService
{
    /// <summary>Alle konfigurierten Module, deren Container tatsächlich existiert.</summary>
    Task<List<ModuleStatus>> GetStatusAsync(CancellationToken ct = default);
    Task StartAsync(string moduleKey, CancellationToken ct = default);
    Task StopAsync(string moduleKey, CancellationToken ct = default);
}

/// <summary>
/// Die steuerbaren Module ergeben sich aus der Host-Konfiguration ("ServiceUrls") - genau die
/// Module, die der Host kennt und die in der Compose-Datei enthalten sind. Postgres und Host
/// tauchen dort nicht auf und sind deshalb nie steuerbar. Abhängigkeiten und Hinweise kommen aus
/// "ModuleControl:Requires:{Modul}" (kommagetrennt) und "ModuleControl:StopHints:{Modul}".
/// </summary>
public class ModuleControlService : IModuleControlService
{
    private readonly IDockerControlService _docker;
    private readonly IConfiguration _configuration;
    private readonly ModuleRegistry _registry;
    private readonly IManuallyStoppedModules _manuallyStopped;
    private readonly string _containerNamePattern;

    public ModuleControlService(
        IDockerControlService docker,
        IConfiguration configuration,
        ModuleRegistry registry,
        IManuallyStoppedModules manuallyStopped)
    {
        _docker = docker;
        _configuration = configuration;
        _registry = registry;
        _manuallyStopped = manuallyStopped;
        _containerNamePattern = configuration["DockerControl:ContainerNamePattern"] ?? "kuestencode_werkbank_{0}";
    }

    public async Task<List<ModuleStatus>> GetStatusAsync(CancellationToken ct = default)
    {
        var tasks = ConfiguredModules().Select(async module =>
        {
            var containerName = ContainerNameFor(module);
            var state = await _docker.GetStateAsync(containerName, ct);
            return state == null ? null : new ModuleStatus(module, containerName, state);
        });

        return (await Task.WhenAll(tasks)).OfType<ModuleStatus>()
            .OrderBy(s => s.Module.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task StartAsync(string moduleKey, CancellationToken ct = default)
    {
        var statuses = await GetStatusAsync(ct);
        var module = Find(statuses, moduleKey).Module;

        var missing = module.Requires
            .Where(required => !statuses.Any(s => s.IsRunning && KeyEquals(s.Module.Key, required)))
            .Select(required => statuses.FirstOrDefault(s => KeyEquals(s.Module.Key, required))?.Module.DisplayName ?? required)
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"{module.DisplayName} benötigt {string.Join(" und ", missing)}. Bitte zuerst {(missing.Count == 1 ? "dieses Modul" : "diese Module")} starten.");

        await _docker.StartAsync(ContainerNameFor(module), ct);
        _manuallyStopped.Clear(module.Key);
    }

    public async Task StopAsync(string moduleKey, CancellationToken ct = default)
    {
        var module = Find(await GetStatusAsync(ct), moduleKey).Module;
        await _docker.StopAsync(ContainerNameFor(module), ct: ct);
        _manuallyStopped.Mark(module.Key, module.DisplayName);
        _registry.UnregisterModule(module.Key);
    }

    public static List<string> GetStopWarnings(string moduleKey, IEnumerable<ModuleStatus> statuses)
    {
        var all = statuses.ToList();
        var target = all.FirstOrDefault(s => KeyEquals(s.Module.Key, moduleKey));
        if (target == null)
            return new List<string>();

        var warnings = all
            .Where(s => s.IsRunning && s.Module.Requires.Any(r => KeyEquals(r, moduleKey)))
            .Select(s => $"{s.Module.DisplayName} benötigt {target.Module.DisplayName} und funktioniert dann nicht mehr.")
            .ToList();

        if (target.Module.StopHint != null)
            warnings.Add(target.Module.StopHint);

        return warnings;
    }

    private IEnumerable<ManagedModule> ConfiguredModules() =>
        _configuration.GetSection("ServiceUrls").GetChildren().Select(child =>
        {
            var requires = (_configuration[$"ModuleControl:Requires:{child.Key}"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return new ManagedModule(
                child.Key,
                _registry.GetModule(child.Key)?.DisplayName is { Length: > 0 } displayName ? displayName : child.Key,
                requires,
                _configuration[$"ModuleControl:StopHints:{child.Key}"]);
        });

    private string ContainerNameFor(ManagedModule module) => string.Format(_containerNamePattern, module.Key.ToLowerInvariant());

    private static bool KeyEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private static ModuleStatus Find(IEnumerable<ModuleStatus> statuses, string moduleKey) =>
        statuses.FirstOrDefault(s => KeyEquals(s.Module.Key, moduleKey))
        ?? throw new ArgumentException($"Unbekanntes oder nicht installiertes Modul '{moduleKey}'", nameof(moduleKey));
}
