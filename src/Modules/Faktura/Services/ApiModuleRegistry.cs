using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Faktura.Services;

/// <summary>
/// Stub implementation of IModuleRegistry for API mode.
/// In API mode, module registration happens via HTTP to the Host,
/// so this implementation does nothing locally.
/// </summary>
public class ApiModuleRegistry : IModuleRegistry
{
    public void RegisterModule(ModuleInfoDto moduleInfo)
    {
        // No-op: Module registration happens via HTTP to Host
    }

    public void UnregisterModule(string moduleName)
    {
        // No-op: Module unregistration happens via HTTP to Host
    }

    public List<ModuleInfoDto> GetAllModules()
    {
        return new List<ModuleInfoDto>();
    }

    public ModuleInfoDto? GetModule(string moduleName)
    {
        return null;
    }
}
