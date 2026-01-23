namespace Kuestencode.Shared.Contracts.Navigation;

public interface IModuleRegistry
{
    void RegisterModule(ModuleInfoDto moduleInfo);
    void UnregisterModule(string moduleName);
    List<ModuleInfoDto> GetAllModules();
    ModuleInfoDto? GetModule(string moduleName);
}
