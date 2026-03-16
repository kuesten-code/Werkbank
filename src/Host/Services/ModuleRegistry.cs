using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Services;

public class ModuleRegistry : Kuestencode.Shared.Contracts.Navigation.IModuleRegistry
{
    private readonly List<ModuleInfoDto> _modules = [];
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
            // Remove existing module with same name
            var existing = _modules.FirstOrDefault(m => m.ModuleName == moduleInfo.ModuleName);
            if (existing != null)
            {
                _modules.Remove(existing);
            }

            _modules.Add(moduleInfo);
        }

        OnChanged?.Invoke();
    }

    public void UnregisterModule(string moduleName)
    {
        lock (_lock)
        {
            var existing = _modules.FirstOrDefault(m => m.ModuleName == moduleName);
            if (existing != null)
            {
                _modules.Remove(existing);
            }
        }

        OnChanged?.Invoke();
    }

    public List<ModuleInfoDto> GetAllModules()
    {
        lock (_lock)
        {
            return _modules.ToList();
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
