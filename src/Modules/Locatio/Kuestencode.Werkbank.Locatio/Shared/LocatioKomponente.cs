using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Kuestencode.Werkbank.Locatio.Shared;

/// <summary>
/// Basis für Seiten: Validierungsfehler der Services erscheinen einheitlich als Snackbar.
/// </summary>
public abstract class LocatioKomponente : ComponentBase
{
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IDialogService DialogService { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    protected static readonly DialogOptions DialogOptionen = new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseOnEscapeKey = true
    };

    protected async Task<bool> VersucheAsync(Func<Task> aktion, string? erfolg = null)
    {
        try
        {
            await aktion();
            if (erfolg != null)
                Snackbar.Add(erfolg, Severity.Success);
            return true;
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
            return false;
        }
    }

    /// <summary>Öffnet einen Dialog und liefert dessen Ergebnis, oder null bei Abbruch.</summary>
    protected async Task<T?> ZeigeDialogAsync<TDialog, T>(string titel, DialogParameters<TDialog> parameter)
        where TDialog : ComponentBase where T : class
    {
        var dialog = await DialogService.ShowAsync<TDialog>(titel, parameter, DialogOptionen);
        var ergebnis = await dialog.Result;
        return ergebnis is { Canceled: false } ? ergebnis.Data as T : null;
    }

    protected async Task<bool> BestaetigeAsync(string frage) =>
        await DialogService.ShowMessageBox("Bitte bestätigen", frage, yesText: "Ja", cancelText: "Abbrechen") == true;

    protected void Herunterladen(string pfad) => Navigation.NavigateTo(pfad, forceLoad: true);

    protected static string Euro(decimal betrag) => betrag.ToString("C");

    protected static string Datum(DateOnly? datum) => datum?.ToString("dd.MM.yyyy") ?? "–";
}
