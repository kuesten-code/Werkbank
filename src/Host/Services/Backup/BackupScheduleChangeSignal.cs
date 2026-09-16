namespace Kuestencode.Werkbank.Host.Services.Backup;

/// <summary>
/// Muss als Singleton registriert sein: <see cref="BackupSchedulerService"/> (ebenfalls
/// Singleton) und <see cref="BackupService"/> (scoped, pro UI-Speichervorgang neu instanziiert)
/// müssen dieselbe Instanz teilen, sonst kommt das Signal nie an.
/// </summary>
public class BackupScheduleChangeSignal : IBackupScheduleChangeSignal
{
    private CancellationTokenSource _cts = new();

    public CancellationToken Token => _cts.Token;

    public void SignalChanged()
    {
        var previous = _cts;
        _cts = new CancellationTokenSource();
        previous.Cancel();
        previous.Dispose();
    }
}
