using System.Text.Json;

namespace Kuestencode.Werkbank.Host.Services.Docker;

/// <summary>
/// Merkt sich, welche Module über die Modulsteuerung bewusst gestoppt wurden, damit die
/// Startseite sie von ausgefallenen Modulen unterscheiden kann. Ein gestopptes Modul meldet sich
/// nach einem Host-Neustart nicht mehr an - deshalb wird der Anzeigename mit abgelegt.
/// </summary>
public interface IManuallyStoppedModules
{
    IReadOnlyDictionary<string, string> All { get; }
    bool IsStopped(string moduleKey);
    void Mark(string moduleKey, string displayName);
    void Clear(string moduleKey);
}

public class ManuallyStoppedModules : IManuallyStoppedModules
{
    private readonly string _filePath;
    private readonly Dictionary<string, string> _stopped;
    private readonly object _lock = new();

    public ManuallyStoppedModules(string filePath)
    {
        _filePath = filePath;
        _stopped = Load(filePath);
    }

    public IReadOnlyDictionary<string, string> All
    {
        get
        {
            lock (_lock)
                return new Dictionary<string, string>(_stopped, StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool IsStopped(string moduleKey)
    {
        lock (_lock)
            return _stopped.ContainsKey(moduleKey);
    }

    public void Mark(string moduleKey, string displayName)
    {
        lock (_lock)
        {
            _stopped[moduleKey] = displayName;
            Save();
        }
    }

    public void Clear(string moduleKey)
    {
        lock (_lock)
        {
            if (_stopped.Remove(moduleKey))
                Save();
        }
    }

    private static Dictionary<string, string> Load(string filePath)
    {
        var stopped = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
            return stopped;

        try
        {
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath));
            foreach (var (key, name) in loaded ?? new())
                stopped[key] = name;
        }
        catch (JsonException)
        {
            // Beschädigte Datei: wie "nichts gemerkt" behandeln - schlimmstenfalls zeigt die
            // Startseite ein gestopptes Modul als "Nicht erreichbar" statt "manuell gestoppt".
        }

        return stopped;
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        File.WriteAllText(_filePath, JsonSerializer.Serialize(_stopped));
    }
}
