using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Kuestencode.Faktura.Shared;
using Kuestencode.Faktura.Shared.Components;
using Kuestencode.Shared.UI.Pages;

namespace Kuestencode.Faktura.Pages.Invoices;

public partial class List : TabbedListPageBase
{
    protected override string PageRoute => "/faktura/invoices";
    protected override string?[] TabStatusKeys => [null, "draft", "sent", "paid", "partiallypaid", "overdue"];

    private List<Invoice> _invoices = new();
    private bool _loading = true;
    private int _draftCount, _sentCount, _paidCount, _partiallyPaidCount, _overdueCount;
    private System.Globalization.CultureInfo _culture = new System.Globalization.CultureInfo("de-DE");
    private TableSortSync _sort = null!;

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
                4 => filtered.Where(i => i.Status == InvoiceStatus.PartiallyPaid),
                5 => filtered.Where(i => i.Status == InvoiceStatus.Overdue),
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
        _sort = new TableSortSync(NavigationManager);
        await LoadInvoices();
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
        _partiallyPaidCount = _invoices.Count(i => i.Status == InvoiceStatus.PartiallyPaid);
        _overdueCount = _invoices.Count(i => i.Status == InvoiceStatus.Overdue);
    }

    protected override Task OnTabChanged() => base.OnTabChanged();

    private void CreateInvoice()
    {
        NavigationManager.NavigateTo("/faktura/invoices/create");
    }

    private void ViewInvoice(int id)
    {
        NavigationManager.NavigateTo(WithFromParam($"/faktura/invoices/details/{id}"));
    }

    private void EditInvoice(int id)
    {
        NavigationManager.NavigateTo($"/faktura/invoices/edit/{id}");
    }

    private Invoice? _markAsPaidInvoice = null;
    private DateTime? _paidDateInput = DateTime.Today;

    private void OpenMarkAsPaidDialog(Invoice invoice)
    {
        _paidDateInput = DateTime.Today;
        _markAsPaidInvoice = invoice;
    }

    private void CloseMarkAsPaidDialog()
    {
        _markAsPaidInvoice = null;
    }

    private async Task ConfirmMarkAsPaid()
    {
        if (_markAsPaidInvoice == null) return;

        var invoice = _markAsPaidInvoice;
        var paidDate = (_paidDateInput ?? DateTime.Today).Date;
        _markAsPaidInvoice = null;

        try
        {
            await InvoiceService.MarkAsPaidAsync(invoice.Id, paidDate);
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
            InvoiceStatus.PartiallyPaid => Color.Warning,
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
            InvoiceStatus.PartiallyPaid => "Teilgezahlt",
            _ => status.ToString()
        };
    }

    private async Task DownloadPdf(Invoice invoice)
    {
        try
        {
            var pdfBytes = await PdfGeneratorService.GenerateInvoicePdfAsync(invoice.Id);
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
