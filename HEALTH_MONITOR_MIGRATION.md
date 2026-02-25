# Module Health Monitor Migration

Alle Module müssen den Health Monitor hinzufügen, damit sie sich automatisch neu registrieren, wenn 60 Sekunden lang kein Health-Check vom Host kommt.

## Änderungen pro Modul

### 1. Using-Direktive hinzufügen

In `ProgramApi.cs` oben:
```csharp
using Kuestencode.Shared.UI.Extensions;
```

### 2. Health Monitor zum ServiceCollection hinzufügen

Nach `AddRapportModule()` / `AddFakturaModule()` etc.:
```csharp
// Add Module Health Monitor - re-registers if no health check received within 60 seconds
builder.Services.AddModuleHealthMonitor("[ModuleName]", GetModuleInfo, builder.Configuration);
```

### 3. Middleware zur Pipeline hinzufügen

Nach `app.UseStaticFiles()`:
```csharp
// Add Health Check Tracker Middleware
app.UseModuleHealthMonitor();
```

### 4. GetModuleInfo-Methode extrahieren

Ersetze die `RegisterWithHost`-Methode mit zwei Methoden:

```csharp
private static ModuleInfoDto GetModuleInfo(IConfiguration config)
{
    var selfUrl = config.GetValue<string>("ServiceUrls:Self") ?? "http://localhost:XXXX";
    var moduleVersion = config["MODULE_VERSION"]
        ?? config["IMAGE_TAG"]
        ?? config["DOCKER_IMAGE_TAG"]
        ?? "dev";

    return new ModuleInfoDto
    {
        ModuleName = "[ModuleName]",
        DisplayName = "[DisplayName]",
        Version = moduleVersion,
        LogoUrl = "/[module]/company/logos/[Module]_Logo.png",
        HealthCheckUrl = $"{selfUrl}/[module]/health",
        NavigationItems = new List<NavItemDto>
        {
            // ... alle bestehenden Navigation Items ...
        }
    };
}

private static async Task RegisterWithHost(IConfiguration config, ILogger logger)
{
    try
    {
        var hostUrl = config.GetValue<string>("ServiceUrls:Host") ?? "http://localhost:8080";
        using var client = new HttpClient { BaseAddress = new Uri(hostUrl) };

        var moduleInfo = GetModuleInfo(config);

        var response = await client.PostAsJsonAsync("/api/modules/register", moduleInfo);
        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation("[ModuleName] module registered successfully with Host at {HostUrl}", hostUrl);
        }
        else
        {
            logger.LogWarning("Failed to register [ModuleName] module with Host. Status: {StatusCode}", response.StatusCode);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error registering [ModuleName] module with Host");
    }
}
```

## Module (mit Ports)

- ✅ Rapport (8082) - Bereits migriert
- ✅ Faktura (8081) - Migriert
- ✅ Offerte (8083) - Migriert
- ✅ Acta (8084) - Migriert
- ✅ Recepta (8085) - Migriert

## Wie es funktioniert

1. **ModuleHealthMonitor** (BackgroundService):
   - Tracked wann der letzte Health-Check vom Host kam
   - Prüft alle 10 Sekunden
   - Wenn 60 Sekunden kein Health-Check: automatische Re-Registrierung

2. **HealthCheckTrackerMiddleware**:
   - Middleware die bei jedem `/health` Request den Monitor benachrichtigt
   - Aktualisiert den `_lastHealthCheckTime`

3. **Host Health Check Service**:
   - Prüft alle 30 Sekunden alle Module
   - Bei fehlgeschlagenen Health-Checks: Modul wird aus Registry entfernt
   - Module merken das (kein Health-Check mehr) und registrieren sich neu
