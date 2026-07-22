using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using MudBlazor;

namespace Kuestencode.Shared.UI.Pages;

/// <summary>
/// Synchronisiert die aktive Sortierspalte und -richtung einer <see cref="MudTable{T}"/> bidirektional
/// mit URL-Query-Parametern (analog zu ?status=/?search= in <see cref="TabbedListPageBase"/>).
/// Bei mehreren Tabellen auf einer Seite muss jede Instanz ein eigenes <c>prefix</c> erhalten.
/// </summary>
public sealed class TableSortSync
{
    private readonly NavigationManager _navigationManager;
    private readonly string _columnKey;
    private readonly string _directionKey;

    public string? Column { get; private set; }
    public SortDirection Direction { get; private set; } = SortDirection.None;

    public TableSortSync(NavigationManager navigationManager, string prefix = "sort")
    {
        _navigationManager = navigationManager;
        _columnKey = $"{prefix}By";
        _directionKey = $"{prefix}Dir";
        ReadFromUrl();
    }

    public void ReadFromUrl()
    {
        var query = QueryHelpers.ParseQuery(new Uri(_navigationManager.Uri).Query);
        Column = query.TryGetValue(_columnKey, out var column) ? column.ToString() : null;
        Direction = Column != null && query.TryGetValue(_directionKey, out var direction) && direction == "desc"
            ? SortDirection.Descending
            : Column != null ? SortDirection.Ascending : SortDirection.None;
    }

    /// <summary>
    /// Liefert die initiale Sortierrichtung für eine Spalte: die aus der URL gelesene Richtung,
    /// falls diese Spalte aktiv ist; andernfalls <paramref name="fallback"/> nur solange kein
    /// Sortierparameter in der URL vorhanden ist (sonst <see cref="SortDirection.None"/>).
    /// </summary>
    public SortDirection InitialDirectionFor(string column, SortDirection fallback = SortDirection.None)
    {
        if (Column == column)
        {
            return Direction;
        }

        return Column == null ? fallback : SortDirection.None;
    }

    public void Update(string column, SortDirection direction)
    {
        var uri = new Uri(_navigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query)
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

        query.Remove(_columnKey);
        query.Remove(_directionKey);
        if (direction != SortDirection.None)
        {
            query[_columnKey] = column;
            query[_directionKey] = direction == SortDirection.Descending ? "desc" : "asc";
        }

        Column = direction != SortDirection.None ? column : null;
        Direction = direction;

        var newUrl = QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path), query!);
        _navigationManager.NavigateTo(newUrl, replace: true);
    }
}
