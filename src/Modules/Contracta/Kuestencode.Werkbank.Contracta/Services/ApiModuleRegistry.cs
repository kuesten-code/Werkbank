using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Contracta.Services;

public class ApiModuleRegistry : IModuleRegistry
{
    public void RegisterModule(ModuleInfoDto moduleInfo) { }
    public void UnregisterModule(string moduleName) { }
    public List<ModuleInfoDto> GetAllModules() => new();
    public ModuleInfoDto? GetModule(string moduleName) => null;
}
