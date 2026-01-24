using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using MudBlazor;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Kuestencode.Faktura.Shared;
using Kuestencode.Faktura.Shared.Components;

namespace Kuestencode.Faktura.Pages.Invoices;

public partial class List
{
    private List<Invoice> _invoices = new();
    private string _searchString = string.Empty;
    private bool _loading = true;
    private int _activeTabIndex = 0;
    private int _draftCount, _sentCount, _paidCount, _overdueCount;
    private System.Globalization.CultureInfo _culture = new System.Globalization.CultureInfo("de-DE");

    private IEnumerable<Invoice> _filteredInvoices
    {
        get
        {
            var filtered = _invoices.AsEnumerable();

            // Filter by tab
            filtered = _activeTabIndex switch
            {
                1 => filtered.Where(i => i.Status == InvoiceStatus.Draft),
                2 => filtered.Where(i => i.Status == InvoiceStatus.Sent),
                3 => filtered.Where(i => i.Status == InvoiceStatus.Paid),
                4 => filtered.Where(i => i.Status == InvoiceStatus.Overdue),
                _ => filtered
            };

            // Filter by search
            if (!string.IsNullOrWhiteSpace(_searchString))
            {
                filtered = filtered.Where(i =>
                    i.InvoiceNumber.Contains(_searchString, StringComparison.OrdinalIgnoreCase) ||
                    (i.Customer != null && i.Customer.Name.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
                );
            }

            return filtered;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoices();
        ApplyQueryParameters();
    }

    private void ApplyQueryParameters()
    {
        var uri = new Uri(NavigationManager.Uri);
        var queryParams = QueryHelpers.ParseQuery(uri.Query);

        // Check for status parameter
        if (queryParams.TryGetValue("status", out var statusParam))
        {
            var status = statusParam.ToString().ToLower();
            _activeTabIndex = status switch
            {
                "draft" => 1,
                "sent" => 2,
                "overdue" => 4,
                _ => 0
            };
        }

        // Check for range parameter (for paid invoices in current month)
        if (queryParams.TryGetValue("range", out var rangeParam))
        {
            var range = rangeParam.ToString().ToLower();
            if (range == "thismonth")
            {
                _activeTabIndex = 3; // Paid tab
            }
        }
    }

    private async Task LoadInvoices()
    {
        _loading = true;
        try
        {
            _invoices = await InvoiceService.GetAllAsync();
            UpdateStatusCounts();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Laden der Rechnungen: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void UpdateStatusCounts()
    {
        _draftCount = _invoices.Count(i => i.Status == InvoiceStatus.Draft);
        _sentCount = _invoices.Count(i => i.Status == InvoiceStatus.Sent);
        _paidCount = _invoices.Count(i => i.Status == InvoiceStatus.Paid);
        _overdueCount = _invoices.Count(i => i.Status == InvoiceStatus.Overdue);
    }

    private Task OnTabChanged()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void CreateInvoice()
    {
        NavigationManager.NavigateTo("/faktura/invoices/create");
    }

    private void ViewInvoice(int id)
    {
        NavigationManager.NavigateTo($"/faktura/invoices/details/{id}");
    }

    private void EditInvoice(int id)
    {
        NavigationManager.NavigateTo($"/faktura/invoices/edit/{id}");
    }

    private async Task MarkAsPaid(Invoice invoice)
    {
        try
        {
            await InvoiceService.MarkAsPaidAsync(invoice.Id);
            Snackbar.Add($"Rechnung {invoice.InvoiceNumber} wurde als bezahlt markiert.", Severity.Success);
            await LoadInvoices();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler: {ex.Message}", Severity.Error);
        }
    }

    private Color GetStatusColor(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Draft => Color.Default,
            InvoiceStatus.Sent => Color.Info,
            InvoiceStatus.Paid => Color.Success,
            InvoiceStatus.Overdue => Color.Error,
            InvoiceStatus.Cancelled => Color.Dark,
            _ => Color.Default
        };
    }

    private string GetStatusText(InvoiceStatus status)
    {
        return status switch
        {
            InvoiceStatus.Draft => "Entwurf",
            InvoiceStatus.Sent => "Versendet",
            InvoiceStatus.Paid => "Beglichen",
            InvoiceStatus.Overdue => "Überfällig",
            InvoiceStatus.Cancelled => "Storniert",
            _ => status.ToString()
        };
    }

    private async Task DownloadPdf(Invoice invoice)
    {
        try
        {
            var pdfBytes = PdfGeneratorService.GenerateInvoicePdf(invoice.Id);
            var fileName = $"{invoice.InvoiceNumber}.pdf";

            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pdfBytes));
            Snackbar.Add($"PDF für Rechnung {invoice.InvoiceNumber} wurde heruntergeladen.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Generieren des PDFs: {ex.Message}", Severity.Error);
        }
    }
}
