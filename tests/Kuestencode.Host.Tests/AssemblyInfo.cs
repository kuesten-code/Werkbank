using Xunit;

// BackupService hält seinen "läuft bereits"-Schutz in einem statischen Feld (siehe
// BackupService._isRunning) - das muss prozessweit gelten, nicht nur pro Scope. Parallele
// Testklassen würden sich damit gegenseitig ins Gehege kommen, daher hier deaktiviert.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
