using Kuestencode.Werkbank.Host.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class BackupTargetDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public BackupTarget Target { get; set; } = new();

    [Parameter]
    public bool IsEdit { get; set; } = false;

    private string _name = string.Empty;
    private BackupTargetType _type = BackupTargetType.Local;
    private bool _enabled = true;
    private string? _path;
    private string? _host;
    private int? _port;
    private bool _useHttps = true;
    private string? _username;
    private string? _region;
    private string? _accessKey;
    private string _passwordInput = string.Empty;
    private string _privateKeyInput = string.Empty;
    private string _secretKeyInput = string.Empty;

    protected override void OnInitialized()
    {
        _name = Target.Name;
        _type = Target.Type;
        _enabled = Target.Enabled;
        _path = Target.Path;
        _host = Target.Host;
        _port = Target.Port;
        _useHttps = Target.UseHttps;
        _username = Target.Username;
        _region = Target.Region;
        _accessKey = Target.AccessKey;

        // Sinnvoller Default statt leerem Feld: /mnt/backup aus der ursprünglichen Spec
        // existiert in unserem docker-compose.yml gar nicht als Mount und "funktioniert"
        // trotzdem scheinbar (Directory.CreateDirectory legt es einfach im Container an) -
        // das Backup landet dann aber nie auf der echten Platte.
        if (!IsEdit && string.IsNullOrWhiteSpace(_path) && _type == BackupTargetType.Local)
        {
            _path = "/app/backup-target";
        }
    }

    private bool CanSave => !string.IsNullOrWhiteSpace(_name) && _type switch
    {
        BackupTargetType.Local => !string.IsNullOrWhiteSpace(_path),
        BackupTargetType.Sftp => !string.IsNullOrWhiteSpace(_host) && !string.IsNullOrWhiteSpace(_username),
        BackupTargetType.S3 => !string.IsNullOrWhiteSpace(_path) && !string.IsNullOrWhiteSpace(_accessKey)
            && (IsEdit || !string.IsNullOrWhiteSpace(_secretKeyInput)),
        BackupTargetType.WebDav => !string.IsNullOrWhiteSpace(_host) && !string.IsNullOrWhiteSpace(_username),
        _ => false
    };

    private void Cancel() => MudDialog.Cancel();

    private void Save()
    {
        Target.Name = _name.Trim();
        Target.Type = _type;
        Target.Enabled = _enabled;
        Target.Path = _path;
        Target.Host = _host;
        Target.Port = _port;
        Target.UseHttps = _type == BackupTargetType.WebDav ? _useHttps : Target.UseHttps;
        Target.Username = _username;
        Target.Region = _region;
        Target.AccessKey = _accessKey;

        if (!string.IsNullOrWhiteSpace(_passwordInput))
            Target.Password = _passwordInput;
        if (!string.IsNullOrWhiteSpace(_privateKeyInput))
            Target.PrivateKey = _privateKeyInput;
        if (!string.IsNullOrWhiteSpace(_secretKeyInput))
            Target.SecretKey = _secretKeyInput;

        MudDialog.Close(DialogResult.Ok(Target));
    }
}
