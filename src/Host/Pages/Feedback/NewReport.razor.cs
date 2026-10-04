using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Models.Feedback;
using Kuestencode.Werkbank.Host.Services;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Kuestencode.Werkbank.Host.Pages.Feedback;

public partial class NewReport
{
    private const string FileInputId = "feedback-file-input";
    private const string GeneralModule = "Werkbank (allgemein)";

    [Inject] private IFeedbackClientService ClientService { get; set; } = default!;
    [Inject] private IFeedbackModeState ModeState { get; set; } = default!;
    [Inject] private ModuleRegistry ModuleRegistry { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    /// <summary>Seite, von der aus gemeldet wurde (vom "Problem melden"-Button übergeben).</summary>
    [SupplyParameterFromQuery(Name = "page")]
    public string? SourcePage { get; set; }

    private readonly CreateFeedbackReportRequest _request = new() { Type = FeedbackType.Bug };
    private readonly List<SelectedFile> _files = new();
    private List<string> _modules = new();
    private List<string> _errors = new();
    private bool _saving;
    private bool _pasteRegistered;

    private bool IsBug => _request.Type == FeedbackType.Bug;

    private record SelectedFile(FeedbackUpload Upload, string? PreviewUrl);

    protected override void OnInitialized()
    {
        _modules = ModuleRegistry.GetAllModules()
            .Select(m => m.ModuleName)
            .OrderBy(name => name)
            .Prepend(GeneralModule)
            .ToList();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || ModeState.Role != FeedbackRole.Client)
            return;

        await JSRuntime.InvokeVoidAsync("werkbankFeedback.registerPaste", FileInputId);
        _pasteRegistered = true;

        if (string.IsNullOrEmpty(SourcePage))
            SourcePage = await JSRuntime.InvokeAsync<string>("werkbankFeedback.referrer");
    }

    private async Task OnFilesSelectedAsync(InputFileChangeEventArgs e)
    {
        _errors = new List<string>();
        var maxBytes = FeedbackUploadPolicy.DefaultMaxFileSizeMb * 1024L * 1024L;

        foreach (var file in e.GetMultipleFiles(FeedbackUploadPolicy.MaxFiles))
        {
            if (file.Size > maxBytes)
            {
                _errors.Add($"Datei '{file.Name}' ist größer als {FeedbackUploadPolicy.DefaultMaxFileSizeMb} MB.");
                continue;
            }

            using var buffer = new MemoryStream();
            await file.OpenReadStream(maxBytes).CopyToAsync(buffer);
            var content = buffer.ToArray();

            var detected = FeedbackUploadPolicy.DetectType(content);
            if (detected == null)
            {
                _errors.Add($"Datei '{file.Name}' hat keinen erlaubten Typ (png, jpg, webp, pdf).");
                continue;
            }

            var preview = detected.Value.ContentType.StartsWith("image/")
                ? $"data:{detected.Value.ContentType};base64,{Convert.ToBase64String(content)}"
                : null;
            _files.Add(new SelectedFile(new FeedbackUpload(file.Name, content), preview));
        }
    }

    private async Task SubmitAsync()
    {
        // Schutz gegen Doppelklick: jede Absendung erzeugt eine neue ClientReportId.
        if (_saving)
            return;

        _saving = true;
        _errors = new List<string>();
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            _request.ReporterName = authState.User.Identity?.Name ?? "Unbekannt";
            _request.PageUrl = SourcePage ?? "";
            _request.AppVersion = HostVersion.Get(Configuration);
            if (!IsBug)
                _request.Expected = null;

            await ClientService.CreateReportAsync(_request, _files.Select(f => f.Upload).ToList());

            Snackbar.Add("Danke! Ihre Meldung wurde gespeichert und wird an den Support übertragen.", Severity.Success);
            NavigationManager.NavigateTo("/feedback");
        }
        catch (FeedbackValidationException ex)
        {
            _errors = ex.Errors.ToList();
        }
        finally
        {
            _saving = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_pasteRegistered)
            return;

        try
        {
            await JSRuntime.InvokeVoidAsync("werkbankFeedback.unregisterPaste");
        }
        catch (JSDisconnectedException)
        {
            // Circuit bereits getrennt — der Listener stirbt mit der Seite.
        }
    }
}
