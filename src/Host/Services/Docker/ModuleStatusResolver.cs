using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Services.Docker;

public enum ModuleDisplayState
{
    Online,
    Offline,
    ManuallyStopped
}

public static class ModuleStatusResolver
{
    /// <summary>
    /// Kombiniert den Registry-Stand (läuft / nicht erreichbar) mit den über die Modulsteuerung
    /// bewusst gestoppten Modulen. Ein manuell gestopptes Modul fehlt nach einem Host-Neustart in der
    /// Registry (es meldet sich nicht mehr an) und wird deshalb aus dem gemerkten Namen ergänzt.
    /// </summary>
    public static List<(string Name, ModuleDisplayState State)> Resolve(
        IEnumerable<(ModuleInfoDto Module, bool IsOnline)> registered,
        IManuallyStoppedModules manuallyStopped)
    {
        var result = new List<(string Name, ModuleDisplayState State)>();
        var registeredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (module, isOnline) in registered)
        {
            registeredNames.Add(module.ModuleName);

            if (isOnline)
            {
                // Läuft wieder (z.B. per Kommandozeile gestartet): Merker ist veraltet.
                manuallyStopped.Clear(module.ModuleName);
                result.Add((module.DisplayName, ModuleDisplayState.Online));
            }
            else
            {
                result.Add((module.DisplayName, manuallyStopped.IsStopped(module.ModuleName)
                    ? ModuleDisplayState.ManuallyStopped
                    : ModuleDisplayState.Offline));
            }
        }

        foreach (var (key, displayName) in manuallyStopped.All.Where(s => !registeredNames.Contains(s.Key)))
            result.Add((displayName, ModuleDisplayState.ManuallyStopped));

        return result.OrderBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
