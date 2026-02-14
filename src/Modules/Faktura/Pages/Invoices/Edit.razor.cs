using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using Kuestencode.Shared.ApiClients;
using Kuestencode.Shared.Contracts.Navigation;
using Kuestencode.Shared.Contracts.Rapport;

namespace Kuestencode.Faktura.Pages.Invoices;

public partial class Edit
{
    [Parameter]
    public int Id { get; set; }

    [Inject]
    public IRapportApiClient RapportApiClient { get; set; } = null!;

    [Inject]
    public IHostApiClient HostApiClient { get; set; } = null!;

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
    private decimal _totalNet, _totalVat, _totalGross, _totalDownPayments, _amountDue;
    private decimal _discountAmount, _totalNetAfterDiscount;
    private bool _hasDiscount = false;
    private System.Globalization.CultureInfo _culture = new System.Globalization.CultureInfo("de-DE");
    private const long MaxAttachmentSize = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedAttachmentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".csv"
    };

    private bool _attachTimesheet;
    private bool _timesheetUseInvoicePeriod = true;
    private DateTime? _timesheetFromDate;
    private DateTime? _timesheetToDate;
    private decimal? _timesheetHourlyRate;
    private string _timesheetTitle = "Tätigkeitsnachweis";
    private string? _timesheetFileName;
    private TimesheetAttachmentFormat _timesheetFormat = TimesheetAttachmentFormat.Pdf;
    private bool _timesheetGenerating;
    private bool _timesheetAttachmentAdded;
    private bool _rapportAvailable = true;

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

            // Initialize discount toggle
            _hasDiscount = _invoice.DiscountType != DiscountType.None;

            SyncTimesheetRange();
            await CheckRapportAvailabilityAsync();
            if (!_rapportAvailable)
            {
                _attachTimesheet = false;
                _timesheetAttachmentAdded = false;
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


    private async Task CheckRapportAvailabilityAsync()
    {
        try
        {
            var navItems = await HostApiClient.GetNavigationAsync();
            _rapportAvailable = navItems.Any(IsRapportNavItem);
        }
        catch
        {
            _rapportAvailable = false;
        }
    }

    private static bool IsRapportNavItem(NavItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.Href) && item.Href.StartsWith("/rapport", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(item.Label, "Rapport", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (item.Children is { Count: > 0 })
        {
            return item.Children.Any(IsRapportNavItem);
        }

        return false;
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

    private void AddItem()
    {
        if (_invoice == null) return;

        var vatRate = _company?.IsKleinunternehmer == true ? 0 : 19;
        _invoice.Items.Add(new InvoiceItem
        {
            Quantity = 1,
            UnitPrice = 0,
            VatRate = vatRate,
            InvoiceId = _invoice.Id
        });
        RecalculateTotals();
    }

    private void RemoveItem(InvoiceItem item)
    {
        if (_invoice == null || _invoice.Items.Count <= 1) return;

        _invoice.Items.Remove(item);
        RecalculateTotals();
    }

    private void AddDownPayment()
    {
        if (_invoice == null) return;

        _invoice.DownPayments.Add(new DownPayment
        {
            Description = string.Empty,
            Amount = 0,
            PaymentDate = null,
            InvoiceId = _invoice.Id
        });
        StateHasChanged();
    }

    private void RemoveDownPayment(DownPayment downPayment)
    {
        if (_invoice == null) return;

        _invoice.DownPayments.Remove(downPayment);
        RecalculateTotals();
    }

    private void OnDiscountToggle()
    {
        if (_invoice == null) return;

        if (!_hasDiscount)
        {
            // Reset discount when toggled off
            _invoice.DiscountType = DiscountType.None;
            _invoice.DiscountValue = null;
        }
        else
        {
            // Initialize discount when toggled on
            _invoice.DiscountType = DiscountType.Percentage;
            _invoice.DiscountValue = 0;
        }

        RecalculateTotals();
    }

    private void OnServicePeriodChanged()
    {
        if (_timesheetUseInvoicePeriod)
        {
            SyncTimesheetRange();
        }
    }

    private void SyncTimesheetRange()
    {
        var from = _servicePeriodStart ?? _invoiceDate ?? DateTime.Today;
        var to = _servicePeriodEnd ?? _servicePeriodStart ?? _invoiceDate ?? DateTime.Today;
        _timesheetFromDate = from;
        _timesheetToDate = to;
    }

    private string GetDiscountLabel()
    {
        if (_invoice == null) return "Rabatt";

        return _invoice.DiscountType == DiscountType.Percentage
            ? "Rabatt in %"
            : "Rabatt in EUR";
    }

    private void RecalculateTotals()
    {
        if (_invoice == null) return;

        // Ensure all items have the correct VAT rate based on company settings
        var vatRate = _company?.IsKleinunternehmer == true ? 0 : 19;
        foreach (var item in _invoice.Items)
        {
            item.VatRate = vatRate;
        }

        _totalNet = _invoice.Items.Sum(i => i.TotalNet);

        // Calculate discount
        if (_invoice.DiscountType != DiscountType.None && _invoice.DiscountValue.HasValue)
        {
            if (_invoice.DiscountType == DiscountType.Percentage)
            {
                _discountAmount = _totalNet * (_invoice.DiscountValue.Value / 100);
            }
            else
            {
                _discountAmount = _invoice.DiscountValue.Value;
            }
        }
        else
        {
            _discountAmount = 0;
        }

        _totalNetAfterDiscount = _totalNet - _discountAmount;

        // Calculate VAT proportionally after discount
        if (_totalNet > 0)
        {
            var discountRatio = _totalNetAfterDiscount / _totalNet;
            _totalVat = _invoice.Items.Sum(i => i.TotalVat) * discountRatio;
        }
        else
        {
            _totalVat = 0;
        }

        _totalGross = _totalNetAfterDiscount + _totalVat;
        _totalDownPayments = _invoice.DownPayments.Sum(d => d.Amount);
        _amountDue = _totalGross - _totalDownPayments;
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
        if (_invoice.Items.Count == 0 || _invoice.Items.All(i => string.IsNullOrWhiteSpace(i.Description)))
        {
            _errorMessage = "Mindestens eine Position angeben.";
            return;
        }

        if (_invoice.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
        {
            _errorMessage = "Menge muss > 0 sein und Preis darf nicht negativ sein.";
            return;
        }

        // Validate invoice date
        if (_invoiceDate == null)
        {
            _errorMessage = "Rechnungsdatum fehlt.";
            return;
        }

        if (_invoiceDate.Value > DateTime.Today)
        {
            _errorMessage = "Rechnungsdatum darf nicht in der Zukunft liegen.";
            return;
        }

        // Abschläge: Validierung
        if (_invoice.DownPayments.Any(d => d.Amount <= 0))
        {
            _errorMessage = "Abschlag-Betrag muss größer als 0 sein.";
            return;
        }

        if (_invoice.DownPayments.Any(d => string.IsNullOrWhiteSpace(d.Description)))
        {
            _errorMessage = "Beschreibung für Abschläge ist erforderlich.";
            return;
        }

        if (_totalDownPayments > _totalGross)
        {
            _errorMessage = "Abschläge dürfen die Bruttosumme nicht übersteigen.";
            return;
        }

        // Rabatt: Validierung
        if (_invoice.DiscountType != DiscountType.None)
        {
            if (!_invoice.DiscountValue.HasValue || _invoice.DiscountValue.Value <= 0)
            {
                _errorMessage = "Rabatt muss größer als 0 sein.";
                return;
            }

            if (_invoice.DiscountType == DiscountType.Percentage && _invoice.DiscountValue.Value > 100)
            {
                _errorMessage = "Prozentualer Rabatt darf nicht über 100% liegen.";
                return;
            }

            if (_invoice.DiscountType == DiscountType.Absolute && _invoice.DiscountValue.Value > _totalNet)
            {
                _errorMessage = "Absoluter Rabatt darf die Nettosumme nicht übersteigen.";
                return;
            }
        }

        _saving = true;

        try
        {
            _invoice.CustomerId = _selectedCustomer.Id;
            _invoice.InvoiceDate = DateTime.SpecifyKind(_invoiceDate.Value, DateTimeKind.Utc);
            _invoice.ServicePeriodStart = _servicePeriodStart.HasValue ? DateTime.SpecifyKind(_servicePeriodStart.Value, DateTimeKind.Utc) : null;
            _invoice.ServicePeriodEnd = _servicePeriodEnd.HasValue ? DateTime.SpecifyKind(_servicePeriodEnd.Value, DateTimeKind.Utc) : null;
            _invoice.DueDate = _dueDate.HasValue ? DateTime.SpecifyKind(_dueDate.Value, DateTimeKind.Utc) : null;

            if (_attachTimesheet)
            {
                var ok = await EnsureTimesheetAttachmentAsync();
                if (!ok)
                {
                    _saving = false;
                    return;
                }
            }

            // Set invoice status based on asDraft parameter
            _invoice.Status = asDraft ? InvoiceStatus.Draft : InvoiceStatus.Sent;

            await InvoiceService.UpdateAsync(_invoice);

            var statusText = asDraft ? "als Entwurf gespeichert" : "versendet";
            Snackbar.Add($"Faktura {_invoice.InvoiceNumber} wurde {statusText}.", Severity.Success);

            NavigationManager.NavigateTo($"/faktura/invoices/details/{_invoice.Id}");
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
        NavigationManager.NavigateTo($"/faktura/invoices");
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

    private async Task CreateTimesheetAttachmentAsync()
    {
        var ok = await EnsureTimesheetAttachmentAsync();
        if (ok)
        {
            Snackbar.Add("Tätigkeitsnachweis wurde als Anhang hinzugefügt.", Severity.Success);
        }
    }

    private async Task<bool> EnsureTimesheetAttachmentAsync()
    {
        if (_timesheetAttachmentAdded)
        {
            return true;
        }

        if (!_rapportAvailable)
        {
            Snackbar.Add("Rapport Modul ist nicht registriert. Tätigkeitsnachweis kann nicht angehängt werden.", Severity.Warning);
            return false;
        }

        if (_invoice == null || _selectedCustomer == null)
        {
            Snackbar.Add("Bitte zuerst einen Kunden auswählen.", Severity.Warning);
            return false;
        }

        if (_timesheetUseInvoicePeriod)
        {
            SyncTimesheetRange();
        }

        if (!_timesheetFromDate.HasValue || !_timesheetToDate.HasValue)
        {
            Snackbar.Add("Bitte einen Zeitraum für den Tätigkeitsnachweis auswählen.", Severity.Warning);
            return false;
        }

        var request = new TimesheetExportRequestDto
        {
            CustomerId = _selectedCustomer.Id,
            From = _timesheetFromDate.Value.Date,
            To = _timesheetToDate.Value.Date.AddDays(1).AddTicks(-1),
            HourlyRate = _timesheetHourlyRate,
            Title = _timesheetTitle,
            FileName = string.IsNullOrWhiteSpace(_timesheetFileName) ? null : _timesheetFileName.Trim()
        };

        _timesheetGenerating = true;
        try
        {
            byte[] bytes;
            string contentType;

            if (_timesheetFormat == TimesheetAttachmentFormat.Csv)
            {
                bytes = await RapportApiClient.GenerateTimesheetCsvAsync(request);
                contentType = "text/csv";
            }
            else
            {
                bytes = await RapportApiClient.GenerateTimesheetPdfAsync(request);
                contentType = "application/pdf";
            }

            var fileName = BuildTimesheetFileName(request, _selectedCustomer.Name, _timesheetFormat);

            _invoice.Attachments.Add(new InvoiceAttachment
            {
                FileName = fileName,
                ContentType = contentType,
                FileSize = bytes.Length,
                Data = bytes
            });

            _timesheetAttachmentAdded = true;
            return true;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Erstellen des Tätigkeitsnachweises: {ex.Message}", Severity.Error);
            return false;
        }
        finally
        {
            _timesheetGenerating = false;
        }
    }

    private static string BuildTimesheetFileName(TimesheetExportRequestDto request, string customerName, TimesheetAttachmentFormat format)
    {
        var extension = format == TimesheetAttachmentFormat.Csv ? ".csv" : ".pdf";

        if (!string.IsNullOrWhiteSpace(request.FileName))
        {
            var raw = request.FileName.Trim();
            if (!raw.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            {
                raw += extension;
            }

            return SanitizeFileName(raw);
        }

        var title = string.IsNullOrWhiteSpace(request.Title) ? "Tätigkeitsnachweis" : request.Title.Trim();
        var period = request.From.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        var baseName = $"{title}_{customerName}_{period}{extension}";
        return SanitizeFileName(baseName);
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalid, '_');
        }

        return fileName;
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

    private enum TimesheetAttachmentFormat
    {
        Pdf,
        Csv
    }
}



