using Microsoft.AspNetCore.Mvc;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModulesController : ControllerBase
{
    private readonly IModuleRegistry _moduleRegistry;
    private readonly ILogger<ModulesController> _logger;

    public ModulesController(IModuleRegistry moduleRegistry, ILogger<ModulesController> logger)
    {
        _moduleRegistry = moduleRegistry;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<List<ModuleInfoDto>> GetRegisteredModules()
    {
        var modules = _moduleRegistry.GetAllModules();
        return Ok(modules);
    }

    [HttpGet("{moduleName}")]
    public ActionResult<ModuleInfoDto> GetModule(string moduleName)
    {
        var module = _moduleRegistry.GetModule(moduleName);
        if (module == null) return NotFound();
        return Ok(module);
    }

    [HttpPost("register")]
    public IActionResult RegisterModule([FromBody] ModuleInfoDto moduleInfo)
    {
        _logger.LogInformation("Registering module: {ModuleName} v{Version}",
            moduleInfo.ModuleName, moduleInfo.Version);

        _moduleRegistry.RegisterModule(moduleInfo);

        return Ok(new { message = $"Module {moduleInfo.ModuleName} registered successfully" });
    }

    [HttpPost("unregister/{moduleName}")]
    public IActionResult UnregisterModule(string moduleName)
    {
        _logger.LogInformation("Unregistering module: {ModuleName}", moduleName);

        _moduleRegistry.UnregisterModule(moduleName);

        return Ok(new { message = $"Module {moduleName} unregistered successfully" });
    }
}
