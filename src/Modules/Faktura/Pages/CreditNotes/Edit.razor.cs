using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;
using Kuestencode.Faktura.Services;
using Kuestencode.Faktura.Shared;
using Kuestencode.Faktura.Shared.Components;
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Acta;

namespace Kuestencode.Faktura.Pages.CreditNotes;

public partial class Edit
{
    [Parameter]
    public int Id { get; set; }

    [Inject]
    public ModuleAvailabilityService AvailabilityService { get; set; } = null!;

    [Inject]
    public IActaApiClient ActaApiClient { get; set; } = null!;

    private bool _customerError;
    private string? _customerErrorText;
    private MudAutocomplete<Customer>? _customerAuto;
    private Invoice? _invoice;
    private Customer? _selectedCustomer;
    private Company? _company;
    private List<Customer> _customers = new();
    private DateTime? _invoiceDate;
    private DateTime? _servicePeriodStart;
    private DateTime? _servicePeriodEnd;
    private DateTime? _dueDate;
    private bool _loading = true;
    private bool _saving = false;
    private string? _errorMessage;
    private decimal _totalNet, _totalVat, _totalGross;
    private bool _isReverseCharge = false;
    private System.Globalization.CultureInfo _culture = new System.Globalization.CultureInfo("de-DE");
    private const long MaxAttachmentSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".csv"
    };

    private bool _actaAvailable;
    private bool _projectWorkflowAvailable;
    private List<ActaProjectDto> _projects = new();
    private int? _selectedProjectExternalId;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _invoice = await InvoiceService.GetByIdAsync(Id, includeCustomer: true, includeItems: true);

            if (_invoice == null)
            {
                _loading = false;
                return;
            }

            // Load customers and company data
            _customers = await CustomerService.GetAllAsync();
            _company = await CompanyService.GetCompanyAsync();

            // Set selected customer
            _selectedCustomer = _invoice.Customer;

            // Convert DateTime values for date pickers
            _invoiceDate = _invoice.InvoiceDate;
            _servicePeriodStart = _invoice.ServicePeriodStart;
            _servicePeriodEnd = _invoice.ServicePeriodEnd;
            _dueDate = _invoice.DueDate;

            // Initialize reverse-charge toggle
            _isReverseCharge = _invoice.IsReverseCharge;

            // Initialize project selection from existing credit note
            _selectedProjectExternalId = _invoice.ProjectId;

            // Gespeicherte Positionen tragen negative Einzelpreise (Vorzeichenkonvention Gutschrift) -
            // für die Bearbeitung zeigt das Grid positive Werte an.
            foreach (var item in _invoice.Items.Where(i => !i.IsHeader))
            {
                item.UnitPrice = Math.Abs(item.UnitPrice);
            }

            RecalculateTotals();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Laden: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await CheckModuleAvailabilityAsync();
        StateHasChanged();
    }

    private async Task CheckModuleAvailabilityAsync()
    {
        try
        {
            var availability = await AvailabilityService.CheckAsync();
            _actaAvailable = availability.Acta;
            _projectWorkflowAvailable = _actaAvailable;

            if (_projectWorkflowAvailable)
            {
                _projects = await ActaApiClient.GetProjectsAsync();
            }
            else
            {
                _projects.Clear();
                _selectedProjectExternalId = null;
            }
        }
        catch
        {
            _actaAvailable = false;
            _projectWorkflowAvailable = false;
            _projects.Clear();
        }
    }

    private void OnProjectChanged(int? projectExternalId)
    {
        _selectedProjectExternalId = projectExternalId;

        if (!projectExternalId.HasValue)
        {
            return;
        }

        var project = _projects.FirstOrDefault(p => p.Id == projectExternalId.Value);
        if (project == null)
        {
            return;
        }

        _selectedCustomer = _customers.FirstOrDefault(c => c.Id == project.CustomerId);
    }

    private async Task<IEnumerable<Customer>> SearchCustomers(string value, CancellationToken token)
    {
        await Task.CompletedTask; // Async compatibility

        if (string.IsNullOrWhiteSpace(value))
            return _customers;

        return _customers.Where(c =>
            c.Name.Contains(value, StringComparison.OrdinalIgnoreCase) ||
            c.CustomerNumber.Contains(value, StringComparison.OrdinalIgnoreCase)
        );
    }

    private async Task OnCustomerCreated(Customer customer)
    {
        _customers = await CustomerService.GetAllAsync();
        _selectedCustomer = _customers.FirstOrDefault(c => c.Id == customer.Id) ?? customer;
        StateHasChanged();
    }

    private record ItemSection(InvoiceItem? Header, List<InvoiceItem> Items);

    private List<ItemSection> GetItemSections()
    {
        var result = new List<ItemSection>();
        InvoiceItem? currentHeader = null;
        var currentItems = new List<InvoiceItem>();

        foreach (var item in _invoice!.Items)
        {
            if (item.IsHeader)
            {
                result.Add(new ItemSection(currentHeader, currentItems));
                currentHeader = item;
                currentItems = new List<InvoiceItem>();
            }
            else
            {
                currentItems.Add(item);
            }
        }
        result.Add(new ItemSection(currentHeader, currentItems));
        return result.Where(s => s.Header != null || s.Items.Count > 0).ToList();
    }

    private void AddItem()
    {
        if (_invoice == null) return;

        decimal vatRate;
        if (_company?.IsKleinunternehmer == true)
            vatRate = 0;
        else if (_isReverseCharge)
            vatRate = 0;
        else
            vatRate = 19;
        _invoice.Items.Add(new InvoiceItem
        {
            Quantity = 1,
            UnitPrice = 0,
            VatRate = vatRate,
            InvoiceId = _invoice.Id
        });
        ReorderItems();
        RecalculateTotals();
    }

    private void AddHeader()
    {
        if (_invoice == null) return;

        _invoice.Items.Add(new InvoiceItem
        {
            IsHeader = true,
            Description = string.Empty,
            Quantity = 0,
            UnitPrice = 0,
            VatRate = 0,
            InvoiceId = _invoice.Id
        });
        ReorderItems();
    }

    private void RemoveItem(InvoiceItem item)
    {
        if (_invoice == null) return;

        var nonHeaderCount = _invoice.Items.Count(i => !i.IsHeader);
        if (item.IsHeader || nonHeaderCount > 1)
        {
            _invoice.Items.Remove(item);
            ReorderItems();
            RecalculateTotals();
        }
    }

    private void MoveItemUp(InvoiceItem item)
    {
        if (_invoice == null) return;

        var index = _invoice.Items.IndexOf(item);
        if (index > 0)
        {
            _invoice.Items.RemoveAt(index);
            _invoice.Items.Insert(index - 1, item);
            ReorderItems();
            RecalculateTotals();
        }
    }

    private void MoveItemDown(InvoiceItem item)
    {
        if (_invoice == null) return;

        var index = _invoice.Items.IndexOf(item);
        if (index < _invoice.Items.Count - 1)
        {
            _invoice.Items.RemoveAt(index);
            _invoice.Items.Insert(index + 1, item);
            ReorderItems();
            RecalculateTotals();
        }
    }

    private void ReorderItems()
    {
        if (_invoice == null) return;

        for (int i = 0; i < _invoice.Items.Count; i++)
        {
            _invoice.Items[i].Position = i + 1;
        }
    }

    private void OnReverseChargeToggle(bool value)
    {
        if (_invoice == null) return;
        _isReverseCharge = value;
        _invoice.IsReverseCharge = value;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        if (_invoice == null) return;

        // Ensure all items have the correct VAT rate based on company settings
        decimal vatRate;
        if (_company?.IsKleinunternehmer == true)
            vatRate = 0;
        else if (_isReverseCharge)
            vatRate = 0;
        else
            vatRate = 19;
        foreach (var item in _invoice.Items)
        {
            item.VatRate = vatRate;
        }

        _totalNet = _invoice.Items.Sum(i => i.TotalNet);
        _totalVat = _invoice.Items.Sum(i => i.TotalVat);
        _totalGross = _totalNet + _totalVat;
        StateHasChanged();
    }

    private async Task SaveAsync(bool asDraft)
    {
        if (_invoice == null) return;

        _customerError = false;
        _customerErrorText = null;
        _errorMessage = null;

        // Validate customer
        if (_selectedCustomer == null)
        {
            _customerError = true;
            _customerErrorText = "Kunde auswählen";

            if (_customerAuto is not null)
                await _customerAuto.FocusAsync();

            return;
        }

        // Validate items
        var nonHeaderItems = _invoice.Items.Where(i => !i.IsHeader).ToList();
        if (nonHeaderItems.Count == 0 || nonHeaderItems.All(i => string.IsNullOrWhiteSpace(i.Description)))
        {
            _errorMessage = "Mindestens eine Position angeben.";
            return;
        }

        if (nonHeaderItems.Any(i => i.Quantity <= 0))
        {
            _errorMessage = "Menge muss > 0 sein.";
            return;
        }

        if (nonHeaderItems.Any(i => i.UnitPrice <= 0))
        {
            _errorMessage = "Einzelpreis muss größer als 0 sein.";
            return;
        }

        // Validate credit note date
        if (_invoiceDate == null)
        {
            _errorMessage = "Gutschriftdatum fehlt.";
            return;
        }

        if (_invoiceDate.Value > DateTime.Today)
        {
            _errorMessage = "Gutschriftdatum darf nicht in der Zukunft liegen.";
            return;
        }

        _saving = true;

        try
        {
            _invoice.CustomerId = _selectedCustomer.Id;
            _invoice.ProjectId = _selectedProjectExternalId;
            _invoice.InvoiceDate = DateTime.SpecifyKind(_invoiceDate.Value, DateTimeKind.Utc);
            _invoice.ServicePeriodStart = _servicePeriodStart.HasValue ? DateTime.SpecifyKind(_servicePeriodStart.Value, DateTimeKind.Utc) : null;
            _invoice.ServicePeriodEnd = _servicePeriodEnd.HasValue ? DateTime.SpecifyKind(_servicePeriodEnd.Value, DateTimeKind.Utc) : null;
            _invoice.DueDate = _dueDate.HasValue ? DateTime.SpecifyKind(_dueDate.Value, DateTimeKind.Utc) : null;

            // Set credit note status based on asDraft parameter
            _invoice.Status = asDraft ? InvoiceStatus.Draft : InvoiceStatus.Sent;

            foreach (var item in _invoice.Items.Where(i => !i.IsHeader))
            {
                item.UnitPrice = -Math.Abs(item.UnitPrice);
            }

            await InvoiceService.UpdateAsync(_invoice);

            var statusText = asDraft ? "als Entwurf gespeichert" : "versendet";
            Snackbar.Add($"Gutschrift {_invoice.InvoiceNumber} wurde {statusText}.", Severity.Success);

            NavigationManager.NavigateTo($"/faktura/credit-notes/details/{_invoice.Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/faktura/credit-notes");
    }

    private async Task HandleAttachmentUpload(IReadOnlyList<IBrowserFile> files)
    {
        if (_invoice == null) return;

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.Name);
            if (!AllowedAttachmentExtensions.Contains(extension))
            {
                Snackbar.Add($"Dateityp nicht erlaubt: {file.Name}", Severity.Error);
                continue;
            }

            if (file.Size > MaxAttachmentSize)
            {
                Snackbar.Add($"Datei zu groß: {file.Name}", Severity.Error);
                continue;
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await file.OpenReadStream(MaxAttachmentSize).CopyToAsync(memoryStream);

                _invoice.Attachments.Add(new InvoiceAttachment
                {
                    FileName = file.Name,
                    ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType,
                    FileSize = file.Size,
                    Data = memoryStream.ToArray()
                });
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Fehler beim Upload: {file.Name} ({ex.Message})", Severity.Error);
            }
        }
    }

    private void RemoveAttachment(InvoiceAttachment attachment)
    {
        _invoice?.Attachments.Remove(attachment);
    }

    private static string FormatFileSize(long size)
    {
        const long kb = 1024;
        const long mb = 1024 * 1024;

        if (size >= mb)
        {
            return $"{size / (double)mb:F1} MB";
        }

        if (size >= kb)
        {
            return $"{size / (double)kb:F1} KB";
        }

        return $"{size} B";
    }
}
