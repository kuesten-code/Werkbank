using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Services;

public class ModuleRegistry : Kuestencode.Shared.Contracts.Navigation.IModuleRegistry
{
    private readonly List<ModuleInfoDto> _modules = [];
    private readonly HashSet<string> _offlineModules = [];
    private readonly object _lock = new();

    /// <summary>
    /// Fired whenever a module is registered or unregistered.
    /// NavMenu subscribes to this to refresh the navigation without a page reload.
    /// </summary>
    public event Action? OnChanged;

    public void RegisterModule(ModuleInfoDto moduleInfo)
    {
        lock (_lock)
        {
            var existing = _modules.FirstOrDefault(m => m.ModuleName == moduleInfo.ModuleName);
            if (existing != null)
                _modules.Remove(existing);

            _modules.Add(moduleInfo);
            _offlineModules.Remove(moduleInfo.ModuleName);
        }

        OnChanged?.Invoke();
    }

    public void UnregisterModule(string moduleName)
    {
        lock (_lock)
        {
            // Keep the module info but mark it offline so the UI can show it as unreachable
            if (_modules.Any(m => m.ModuleName == moduleName))
                _offlineModules.Add(moduleName);
        }

        OnChanged?.Invoke();
    }

    /// <summary>
    /// Returns only online modules (used by NavMenu / navigation).
    /// </summary>
    public List<ModuleInfoDto> GetAllModules()
    {
        lock (_lock)
        {
            return _modules.Where(m => !_offlineModules.Contains(m.ModuleName)).ToList();
        }
    }

    /// <summary>
    /// Returns all modules that have ever registered, with their online state.
    /// Used by the Systemstatus panel.
    /// </summary>
    public List<(ModuleInfoDto Module, bool IsOnline)> GetAllModulesWithStatus()
    {
        lock (_lock)
        {
            return _modules
                .Select(m => (m, !_offlineModules.Contains(m.ModuleName)))
                .ToList();
        }
    }

    public ModuleInfoDto? GetModule(string moduleName)
    {
        lock (_lock)
        {
            return _modules.FirstOrDefault(m => m.ModuleName == moduleName);
        }
    }
}
