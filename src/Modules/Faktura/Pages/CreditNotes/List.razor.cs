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

namespace Kuestencode.Faktura.Pages.CreditNotes;

public partial class List : TabbedListPageBase
{
    protected override string PageRoute => "/faktura/credit-notes";
    protected override string?[] TabStatusKeys => [null, "draft", "sent", "paid", "partiallypaid", "overdue"];

    private List<Invoice> _creditNotes = new();
    private bool _loading = true;
    private int _draftCount, _sentCount, _paidCount, _partiallyPaidCount, _overdueCount;
    private System.Globalization.CultureInfo _culture = new System.Globalization.CultureInfo("de-DE");
    private TableSortSync _sort = null!;

    private IEnumerable<Invoice> _filteredCreditNotes
    {
        get
        {
            var filtered = _creditNotes.AsEnumerable();

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
        await LoadCreditNotes();
    }

    private async Task LoadCreditNotes()
    {
        _loading = true;
        try
        {
            _creditNotes = await InvoiceService.GetByTypeAsync(InvoiceType.CreditNote);
            UpdateStatusCounts();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Laden der Gutschriften: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void UpdateStatusCounts()
    {
        _draftCount = _creditNotes.Count(i => i.Status == InvoiceStatus.Draft);
        _sentCount = _creditNotes.Count(i => i.Status == InvoiceStatus.Sent);
        _paidCount = _creditNotes.Count(i => i.Status == InvoiceStatus.Paid);
        _partiallyPaidCount = _creditNotes.Count(i => i.Status == InvoiceStatus.PartiallyPaid);
        _overdueCount = _creditNotes.Count(i => i.Status == InvoiceStatus.Overdue);
    }

    protected override Task OnTabChanged() => base.OnTabChanged();

    private void CreateCreditNote()
    {
        NavigationManager.NavigateTo("/faktura/credit-notes/create");
    }

    private void ViewCreditNote(int id)
    {
        NavigationManager.NavigateTo(WithFromParam($"/faktura/credit-notes/details/{id}"));
    }

    private void EditCreditNote(int id)
    {
        NavigationManager.NavigateTo($"/faktura/credit-notes/edit/{id}");
    }

    private Invoice? _markAsPaidCreditNote = null;
    private DateTime? _paidDateInput = DateTime.Today;

    private void OpenMarkAsPaidDialog(Invoice creditNote)
    {
        _paidDateInput = DateTime.Today;
        _markAsPaidCreditNote = creditNote;
    }

    private void CloseMarkAsPaidDialog()
    {
        _markAsPaidCreditNote = null;
    }

    private async Task ConfirmMarkAsPaid()
    {
        if (_markAsPaidCreditNote == null) return;

        var creditNote = _markAsPaidCreditNote;
        var paidDate = (_paidDateInput ?? DateTime.Today).Date;
        _markAsPaidCreditNote = null;

        try
        {
            await InvoiceService.MarkAsPaidAsync(creditNote.Id, paidDate);
            Snackbar.Add($"Gutschrift {creditNote.InvoiceNumber} wurde als beglichen markiert.", Severity.Success);
            await LoadCreditNotes();
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

    private async Task DownloadPdf(Invoice creditNote)
    {
        try
        {
            var pdfBytes = await PdfGeneratorService.GenerateInvoicePdfAsync(creditNote.Id);
            var fileName = $"{creditNote.InvoiceNumber}.pdf";

            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pdfBytes));
            Snackbar.Add($"PDF für Gutschrift {creditNote.InvoiceNumber} wurde heruntergeladen.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Generieren des PDFs: {ex.Message}", Severity.Error);
        }
    }
}
