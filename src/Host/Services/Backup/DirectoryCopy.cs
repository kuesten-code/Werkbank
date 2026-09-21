namespace Kuestencode.Werkbank.Host.Services.Backup;

public static class DirectoryCopy
{
    /// <summary>
    /// Kopiert <paramref name="sourceDir"/> rekursiv nach <paramref name="destinationDir"/> und überschreibt
    /// dabei gleichnamige Dateien. Dateien, die nur im Ziel existieren, bleiben unangetastet.
    /// </summary>
    /// <param name="tolerateVanishedSource">
    /// Für Quellen, die sich während des Kopierens ändern (laufendes Postgres recycelt WAL-Segmente und
    /// löscht temporäre Dateien): Dateien/Ordner, die zwischen Auflisten und Kopieren verschwinden, werden
    /// übersprungen statt als Fehler gewertet.
    /// </param>
    /// <param name="onBeforeCopy">Wird vor dem Kopieren jeder Datei aufgerufen (Testnaht für das Verschwinden von Dateien).</param>
    /// <returns>Anzahl der übersprungenen, während des Kopierens verschwundenen Einträge.</returns>
    public static int CopyOverwriting(
        string sourceDir, string destinationDir, bool tolerateVanishedSource = false, Action<string>? onBeforeCopy = null)
    {
        var skipped = 0;
        Directory.CreateDirectory(destinationDir);

        string[] files;
        string[] subDirs;
        try
        {
            files = Directory.GetFiles(sourceDir);
            subDirs = Directory.GetDirectories(sourceDir);
        }
        catch (DirectoryNotFoundException) when (tolerateVanishedSource)
        {
            return 1;
        }

        foreach (var file in files)
        {
            try
            {
                onBeforeCopy?.Invoke(file);
                File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), overwrite: true);
            }
            catch (Exception ex) when (tolerateVanishedSource && ex is FileNotFoundException or DirectoryNotFoundException)
            {
                skipped++;
            }
        }

        foreach (var subDir in subDirs)
            skipped += CopyOverwriting(subDir, Path.Combine(destinationDir, Path.GetFileName(subDir)), tolerateVanishedSource, onBeforeCopy);

        return skipped;
    }
}
