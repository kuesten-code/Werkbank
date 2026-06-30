using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace Kuestencode.Shared.UI.Pages;

/// <summary>
/// Abstrakte Basisklasse für Übersichtsseiten mit Tab-basierter Statusfilterung.
/// Synchronisiert den aktiven Tab bidirektional mit dem URL-Query-Parameter ?status=,
/// sodass der aktive Filter bei Browser-Navigation (Vor/Zurück) erhalten bleibt.
/// </summary>
public abstract class TabbedListPageBase : ComponentBase, IDisposable
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected int _activeTabIndex;

    /// <summary>Route der Listenseite, z.B. "/faktura/invoices".</summary>
    protected abstract string PageRoute { get; }

    /// <summary>
    /// Statusschlüssel für jeden Tab-Index. Index 0 ist immer null ("Alle").
    /// Beispiel: [null, "draft", "sent", "paid"]
    /// </summary>
    protected abstract string?[] TabStatusKeys { get; }

    protected string? CurrentStatusKey =>
        _activeTabIndex > 0 && _activeTabIndex < TabStatusKeys.Length
            ? TabStatusKeys[_activeTabIndex]
            : null;

    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        SyncTabFromUrl();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        SyncTabFromUrl();
        InvokeAsync(StateHasChanged);
    }

    protected virtual void SyncTabFromUrl()
    {
        var queryParams = QueryHelpers.ParseQuery(new Uri(NavigationManager.Uri).Query);
        if (queryParams.TryGetValue("status", out var statusParam))
        {
            var idx = Array.IndexOf(TabStatusKeys, statusParam.ToString().ToLower());
            _activeTabIndex = idx > 0 ? idx : 0;
        }
        else
        {
            _activeTabIndex = 0;
        }
    }

    protected void SyncTabToUrl()
    {
        var currentPath = new Uri(NavigationManager.Uri).AbsolutePath;
        var url = CurrentStatusKey != null
            ? $"{currentPath}?status={CurrentStatusKey}"
            : currentPath;
        NavigationManager.NavigateTo(url, replace: true);
    }

    protected virtual Task OnTabChanged()
    {
        SyncTabToUrl();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Fügt ?from= Parameter zur Detailseiten-URL hinzu.</summary>
    protected string WithFromParam(string detailUrl) =>
        CurrentStatusKey != null ? $"{detailUrl}?from={CurrentStatusKey}" : detailUrl;

    /// <summary>Erzeugt die Zurück-URL unter Berücksichtigung des from-Parameters.</summary>
    public static string BackUrl(string listRoute, string? fromFilter) =>
        fromFilter != null ? $"{listRoute}?status={fromFilter}" : listRoute;

    public virtual void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
