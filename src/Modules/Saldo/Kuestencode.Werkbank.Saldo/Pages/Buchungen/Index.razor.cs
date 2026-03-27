using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Kuestencode.Werkbank.Saldo.Domain.Dtos;
using Kuestencode.Werkbank.Saldo.Domain.Enums;
using Kuestencode.Werkbank.Saldo.Services;

namespace Kuestencode.Werkbank.Saldo.Pages.Buchungen;

public partial class Index
{
    [Inject] private ISaldoAggregationService SaldoSvc { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private MudTable<BuchungDto>? _table;

    private bool _loading = false;
    private List<BuchungDto> _alle = new();
    private List<BuchungDto> _gefiltert = new();
    private List<string> _kategorien = new();

    private DateTime? _vonDate = new DateTime(DateTime.Today.Year, 1, 1);
    private DateTime? _bisDate = new DateTime(DateTime.Today.Year, 12, 31);
    private string _suchtext = string.Empty;
    private string? _typFilter;
    private string? _kategorieFilter;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        StateHasChanged();
        try
        {
            var von = _vonDate.HasValue
                ? DateOnly.FromDateTime(_vonDate.Value)
                : new DateOnly(DateTime.Today.Year, 1, 1);
            var bis = _bisDate.HasValue
                ? DateOnly.FromDateTime(_bisDate.Value)
                : new DateOnly(DateTime.Today.Year, 12, 31);

            _alle = await SaldoSvc.GetAlleBuchungenAsync(von, bis);

            _kategorien = _alle
                .Where(b => !string.IsNullOrEmpty(b.Kategorie))
                .Select(b => b.Kategorie)
                .Distinct()
                .OrderBy(k => k)
                .ToList();

            ApplyFilter();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Laden: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void OnSuchtextChanged(string v) { _suchtext = v; ApplyFilter(); }
    private void OnTypFilterChanged(string? v) { _typFilter = v; ApplyFilter(); }
    private void OnKategorieFilterChanged(string? v) { _kategorieFilter = v; ApplyFilter(); }

    private void ApplyFilter()
    {
        _gefiltert = _alle
            .Where(b => string.IsNullOrEmpty(_typFilter) || b.Typ.ToString() == _typFilter)
            .Where(b => string.IsNullOrEmpty(_kategorieFilter) || b.Kategorie == _kategorieFilter)
            .Where(b => string.IsNullOrWhiteSpace(_suchtext) ||
                        b.QuelleId.Contains(_suchtext, StringComparison.OrdinalIgnoreCase) ||
                        b.Beschreibung.Contains(_suchtext, StringComparison.OrdinalIgnoreCase) ||
                        b.KontoNummer.Contains(_suchtext, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.ZahlungsDatum)
            .ToList();
    }

    private void OnRowClick(TableRowClickEventArgs<BuchungDto> args)
    {
        var b = args.Item;
        if (b == null) return;

        var url = b.Quelle == "Faktura"
            ? $"/faktura/invoices/{b.Id}"
            : $"/recepta/belege/{b.Id}";

        NavigationManager.NavigateTo(url);
    }

    private async Task ExportCsvAsync()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Datum;Beleg-Nr.;Beschreibung;Kategorie;Konto;Kontobezeichnung;Typ;Netto;USt;Brutto;USt-Satz;Quelle");

        foreach (var b in _gefiltert)
        {
            sb.AppendLine(string.Join(";", new[]
            {
                b.ZahlungsDatum.ToString("dd.MM.yyyy"),
                Escape(b.QuelleId),
                Escape(b.Beschreibung),
                Escape(b.Kategorie),
                b.KontoNummer,
                Escape(b.KontoBezeichnung),
                b.Typ == BuchungsTyp.Einnahme ? "Einnahme" : "Ausgabe",
                b.Netto.ToString("N2"),
                b.Ust.ToString("N2"),
                b.Brutto.ToString("N2"),
                b.UstSatz.ToString("N1") + "%",
                b.Quelle
            }));
        }

        var csvBytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();

        var von = _vonDate.HasValue ? DateOnly.FromDateTime(_vonDate.Value) : new DateOnly(DateTime.Today.Year, 1, 1);
        var bis = _bisDate.HasValue ? DateOnly.FromDateTime(_bisDate.Value) : new DateOnly(DateTime.Today.Year, 12, 31);
        var fileName = $"Buchungen_{von:yyyy-MM-dd}_{bis:yyyy-MM-dd}.csv";

        var base64 = Convert.ToBase64String(csvBytes);
        await JS.InvokeVoidAsync("downloadFileFromBase64", fileName, "text/csv;charset=utf-8", base64);
        Snackbar.Add($"{_gefiltert.Count} Buchungen als CSV exportiert.", Severity.Success);
    }

    private static string GetSaldoStyle(decimal saldo)
        => "text-align:right; font-weight:700; font-size:1rem; color:" + (saldo >= 0 ? "#4caf50" : "#f44336") + ";";

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
