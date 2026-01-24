using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Stub implementation of IModuleRegistry for API mode.
/// In API mode, module registration happens via HTTP to the Host.
/// </summary>
public class ApiModuleRegistry : IModuleRegistry
{
    public void RegisterModule(ModuleInfoDto moduleInfo)
    {
    }

    public void UnregisterModule(string moduleName)
    {
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
