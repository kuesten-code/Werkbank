using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Pages.Invoices;

public partial class Import
{
    private const long MaxPdfSize = 10 * 1024 * 1024;

    private readonly CultureInfo _culture = new("de-DE");
    private List<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private string _invoiceNumber = string.Empty;
    private DateTime? _invoiceDate;
    private DateTime? _dueDate;
    private decimal _netAmount;
    private decimal _vatRate = 19m;
    private decimal _alreadyPaid;
    private DateTime? _alreadyPaidDate;
    private string? _notes;
    private string? _pdfFileName;
    private byte[]? _pdfData;
    private string? _errorMessage;
    private bool _saving;

    private decimal GrossAmount => BuildImport().GrossAmount;

    protected override async Task OnInitializedAsync()
    {
        _customers = await CustomerService.GetAllAsync();
        var company = await CompanyService.GetCompanyAsync();
        if (company.IsKleinunternehmer)
            _vatRate = 0m;
    }

    private Task<IEnumerable<Customer>> SearchCustomers(string value, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Task.FromResult<IEnumerable<Customer>>(_customers);

        return Task.FromResult(_customers.Where(c =>
            c.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
            c.CustomerNumber.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    private async Task OnCustomerCreated(Customer customer)
    {
        _customers = await CustomerService.GetAllAsync();
        _selectedCustomer = _customers.FirstOrDefault(c => c.Id == customer.Id) ?? customer;
    }

    private async Task HandlePdfUpload(IBrowserFile file)
    {
        if (!string.Equals(Path.GetExtension(file.Name), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Snackbar.Add($"Nur PDF-Dateien erlaubt: {file.Name}", Severity.Error);
            return;
        }

        if (file.Size > MaxPdfSize)
        {
            Snackbar.Add($"Datei zu groß: {file.Name}", Severity.Error);
            return;
        }

        using var memoryStream = new MemoryStream();
        await file.OpenReadStream(MaxPdfSize).CopyToAsync(memoryStream);
        _pdfData = memoryStream.ToArray();
        _pdfFileName = file.Name;
    }

    private LegacyInvoiceImport BuildImport() => new(
        _invoiceNumber ?? string.Empty,
        _selectedCustomer?.Id ?? 0,
        _invoiceDate ?? DateTime.Today,
        _dueDate,
        _netAmount,
        _vatRate,
        _alreadyPaid,
        _alreadyPaidDate,
        _pdfFileName ?? string.Empty,
        _pdfData ?? Array.Empty<byte>(),
        _notes);

    private async Task SaveAsync(bool continueWithNext)
    {
        _errorMessage = null;
        if (!_invoiceDate.HasValue)
        {
            _errorMessage = "Rechnungsdatum ist erforderlich.";
            return;
        }

        _saving = true;
        try
        {
            var invoice = await ImportService.ImportAsync(BuildImport());
            Snackbar.Add($"Altrechnung {invoice.InvoiceNumber} wurde übernommen.", Severity.Success);

            if (continueWithNext)
                ResetForNextInvoice();
            else
                NavigationManager.NavigateTo($"/faktura/invoices/details/{invoice.Id}");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    // Kunde und MwSt-Satz bleiben stehen: beim Abarbeiten einer OP-Liste folgen oft mehrere Rechnungen desselben Kunden
    private void ResetForNextInvoice()
    {
        _invoiceNumber = string.Empty;
        _invoiceDate = null;
        _dueDate = null;
        _netAmount = 0;
        _alreadyPaid = 0;
        _alreadyPaidDate = null;
        _notes = null;
        _pdfFileName = null;
        _pdfData = null;
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/faktura/invoices/create");
    }
}
