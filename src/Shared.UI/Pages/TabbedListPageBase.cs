using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace Kuestencode.Shared.UI.Pages;

/// <summary>
/// Abstrakte Basisklasse für Übersichtsseiten mit Tab-basierter Statusfilterung.
/// Synchronisiert aktiven Tab und Suchtext bidirektional mit URL-Query-Parametern
/// ?status= und ?search=, sodass der aktive Filter bei Browser-Navigation erhalten bleibt.
/// </summary>
public abstract class TabbedListPageBase : ComponentBase, IDisposable
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected int _activeTabIndex;
    protected string _searchString = string.Empty;

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
        _searchString = queryParams.TryGetValue("search", out var search) ? search.ToString() : string.Empty;
    }

    protected void SyncTabToUrl()
    {
        var currentPath = new Uri(NavigationManager.Uri).AbsolutePath;
        var parts = new List<string>();
        if (CurrentStatusKey != null) parts.Add($"status={CurrentStatusKey}");
        if (!string.IsNullOrEmpty(_searchString)) parts.Add($"search={Uri.EscapeDataString(_searchString)}");
        var url = parts.Count > 0 ? $"{currentPath}?{string.Join("&", parts)}" : currentPath;
        NavigationManager.NavigateTo(url, replace: true);
    }

    protected virtual Task OnTabChanged()
    {
        SyncTabToUrl();
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>Fügt ?from= und ?fromsearch= Parameter zur Detailseiten-URL hinzu.</summary>
    protected string WithFromParam(string detailUrl)
    {
        var parts = new List<string>();
        if (CurrentStatusKey != null) parts.Add($"from={CurrentStatusKey}");
        if (!string.IsNullOrEmpty(_searchString)) parts.Add($"fromsearch={Uri.EscapeDataString(_searchString)}");
        return parts.Count > 0 ? $"{detailUrl}?{string.Join("&", parts)}" : detailUrl;
    }

    /// <summary>Erzeugt die Zurück-URL unter Berücksichtigung von Status- und Suchfilter.</summary>
    public static string BackUrl(string listRoute, string? fromFilter, string? fromSearch = null)
    {
        var parts = new List<string>();
        if (fromFilter != null) parts.Add($"status={fromFilter}");
        if (!string.IsNullOrEmpty(fromSearch)) parts.Add($"search={Uri.EscapeDataString(fromSearch)}");
        return parts.Count > 0 ? $"{listRoute}?{string.Join("&", parts)}" : listRoute;
    }

    public virtual void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
