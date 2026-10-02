using System.Globalization;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Microsoft.JSInterop;
using MudBlazor;

namespace Kuestencode.Faktura.Pages.OpenItems;

public partial class Index
{
    private readonly CultureInfo _culture = new("de-DE");
    private DateTime? _from = new DateTime(DateTime.Today.Year, 1, 1);
    private DateTime? _to = DateTime.Today;
    private IReadOnlyList<OpenItem>? _items;
    private bool _loading;

    private bool IsRangeValid => _from.HasValue && _to.HasValue && _from <= _to;

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _items = await OpenItemsService.GetOpenItemsAsync(_from!.Value, _to!.Value);
        }
        catch (Exception ex)
        {
            _items = null;
            Snackbar.Add($"Fehler beim Laden der offenen Posten: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DownloadCsvAsync()
    {
        await LoadAsync();
        if (_items == null) return;

        var bytes = OpenItemsCsvWriter.Write(_items);
        var fileName = OpenItemsCsvWriter.FileName(_from!.Value, _to!.Value);
        await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));
        Snackbar.Add($"OP-Liste mit {_items.Count} Posten wurde heruntergeladen.", Severity.Success);
    }
}
