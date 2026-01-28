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

public partial class Create
{
    [Inject]
    public IRapportApiClient RapportApiClient { get; set; } = null!;

    [Inject]
    public IHostApiClient HostApiClient { get; set; } = null!;

    private bool _customerError;
    private string? _customerErrorText;
    private MudAutocomplete<Customer>? _customerAuto;
    private Invoice _invoice = new();
    private Customer? _selectedCustomer;
    private Company? _company;
    private List<Customer> _customers = new();
    private DateTime? _invoiceDate = DateTime.Today;
    private DateTime? _servicePeriodStart;
    private DateTime? _servicePeriodEnd;
    private DateTime? _dueDate = DateTime.Today.AddDays(14);
    private bool _saving = false;
    private string? _errorMessage;
    private decimal _totalNet, _totalVat, _totalGross, _totalDownPayments, _amountDue;
    private decimal _discountAmount, _totalNetAfterDiscount;
    private bool _enableDiscount = false;
    private bool _discountWarning = false;
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
            _invoice.InvoiceNumber = await InvoiceService.GenerateInvoiceNumberAsync();
            _customers = await CustomerService.GetAllAsync();
            _company = await CompanyService.GetCompanyAsync();
            AddItem();
            SyncTimesheetRange();
            RecalculateTotals();
            await CheckRapportAvailabilityAsync();
            if (!_rapportAvailable)
            {
                _attachTimesheet = false;
                _timesheetAttachmentAdded = false;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Initialisieren: {ex.Message}";
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

    private void AddItem()
    {
        var vatRate = _company?.IsKleinunternehmer == true ? 0 : 19;
        _invoice.Items.Add(new InvoiceItem
        {
            Quantity = 1,
            UnitPrice = 0,
            VatRate = vatRate
        });
    }

    private void RemoveItem(InvoiceItem item)
    {
        if (_invoice.Items.Count > 1)
        {
            _invoice.Items.Remove(item);
            RecalculateTotals();
        }
    }

    private void AddDownPayment()
    {
        _invoice.DownPayments.Add(new DownPayment
        {
            Description = string.Empty,
            Amount = 0,
            PaymentDate = null
        });
        StateHasChanged();
    }

    private void RemoveDownPayment(DownPayment downPayment)
    {
        _invoice.DownPayments.Remove(downPayment);
        RecalculateTotals();
    }

    private void OnDiscountToggle()
    {
        if (!_enableDiscount)
        {
            _invoice.DiscountType = DiscountType.None;
            _invoice.DiscountValue = null;
        }
        else
        {
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
        if (_invoice.DiscountType == DiscountType.Percentage && _invoice.DiscountValue.HasValue)
        {
            return $"Rabatt ({_invoice.DiscountValue.Value:N2}%)";
        }
        return "Rabatt";
    }

    private void RecalculateTotals()
    {
        // Ensure all items have the correct VAT rate based on company settings
        var vatRate = _company?.IsKleinunternehmer == true ? 0 : 19;
        foreach (var item in _invoice.Items)
        {
            item.VatRate = vatRate;
        }

        _totalNet = _invoice.TotalNet;
        _discountAmount = _invoice.DiscountAmount;
        _totalNetAfterDiscount = _invoice.TotalNetAfterDiscount;
        _totalVat = _invoice.TotalVat;
        _totalGross = _invoice.TotalGross;
        _totalDownPayments = _invoice.TotalDownPayments;
        _amountDue = _invoice.AmountDue;

        // Check for discount warning
        if (_invoice.DiscountType == DiscountType.Percentage &&
            _invoice.DiscountValue.HasValue &&
            _invoice.DiscountValue.Value > 50)
        {
            _discountWarning = true;
        }
        else if (_invoice.DiscountType == DiscountType.Absolute &&
                 _totalNet > 0 &&
                 _discountAmount / _totalNet > 0.5m)
        {
            _discountWarning = true;
        }
        else
        {
            _discountWarning = false;
        }

        StateHasChanged();
    }

    private async Task<bool> ValidateInvoiceAsync()
    {
        _customerError = false;
        _customerErrorText = null;
        _errorMessage = null;

        // Regel: Kunde ist Pflicht
        if (_selectedCustomer == null)
        {
            _customerError = true;
            _customerErrorText = "Kunde auswählen";

            if (_customerAuto is not null)
                await _customerAuto.FocusAsync();

            return false;
        }

        // Positionen: Pflicht
        if ((_invoice.Items.Count == 0 || _invoice.Items.All(i => string.IsNullOrWhiteSpace(i.Description))))
        {
            _errorMessage = "Mindestens eine Position angeben.";
            return false;
        }

        if (_invoice.Items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
        {
            _errorMessage = "Menge muss > 0 sein und Preis darf nicht negativ sein.";
            return false;
        }

        // Rechnungsdatum: Pflicht
        if (_invoiceDate == null)
        {
            _errorMessage = "Rechnungsdatum fehlt.";
            return false;
        }

        if (_invoiceDate.Value > DateTime.Today)
        {
            _errorMessage = "Rechnungsdatum darf nicht in der Zukunft liegen.";
            return false;
        }

        // Rabatt: Validierung
        if (_enableDiscount && _invoice.DiscountValue.HasValue)
        {
            if (_invoice.DiscountType == DiscountType.Percentage)
            {
                if (_invoice.DiscountValue.Value < 0 || _invoice.DiscountValue.Value > 100)
                {
                    _errorMessage = "Rabatt in Prozent muss zwischen 0 und 100 liegen.";
                    return false;
                }
            }
            else if (_invoice.DiscountType == DiscountType.Absolute)
            {
                if (_invoice.DiscountValue.Value <= 0)
                {
                    _errorMessage = "Rabatt-Betrag muss größer als 0 sein.";
                    return false;
                }
                if (_invoice.DiscountValue.Value > _totalNet)
                {
                    _errorMessage = "Rabatt-Betrag darf die Nettosumme nicht übersteigen.";
                    return false;
                }
            }
        }

        // Abschläge: Validierung
        if (_invoice.DownPayments.Any(d => d.Amount <= 0))
        {
            _errorMessage = "Abschlag-Betrag muss größer als 0 sein.";
            return false;
        }

        if (_invoice.DownPayments.Any(d => string.IsNullOrWhiteSpace(d.Description)))
        {
            _errorMessage = "Beschreibung für Abschläge ist erforderlich.";
            return false;
        }

        if (_totalDownPayments > _totalGross)
        {
            _errorMessage = "Abschläge dürfen die Bruttosumme nicht übersteigen.";
            return false;
        }

        return true;
    }

    private async Task SaveAsync(bool asDraft)
    {
        if (!await ValidateInvoiceAsync())
            return;

        _saving = true;

        try
        {
            if (_selectedCustomer != null)
                _invoice.CustomerId = _selectedCustomer.Id;

            var invoiceDate = _invoiceDate ?? throw new InvalidOperationException("Rechnungsdatum fehlt.");
            _invoice.InvoiceDate = DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc);
            _invoice.ServicePeriodStart = _servicePeriodStart.HasValue ? DateTime.SpecifyKind(_servicePeriodStart.Value, DateTimeKind.Utc) : null;
            _invoice.ServicePeriodEnd   = _servicePeriodEnd.HasValue ? DateTime.SpecifyKind(_servicePeriodEnd.Value, DateTimeKind.Utc) : null;
            _invoice.DueDate            = _dueDate.HasValue ? DateTime.SpecifyKind(_dueDate.Value, DateTimeKind.Utc) : null;

            if (_attachTimesheet)
            {
                var ok = await EnsureTimesheetAttachmentAsync();
                if (!ok)
                {
                    _saving = false;
                    return;
                }
            }

            _invoice.Status = asDraft ? InvoiceStatus.Draft : InvoiceStatus.Sent;

            await InvoiceService.CreateAsync(_invoice);

            Snackbar.Add($"Rechnung {_invoice.InvoiceNumber} wurde erfolgreich erstellt.",
                Severity.Success);

            NavigationManager.NavigateTo("/faktura/invoices");
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

    private async Task SaveAndSendAsync(bool asDraft = false)
    {
        if (!await ValidateInvoiceAsync())
            return;

        // E-Mail-Konfiguration prüfen
        var isEmailConfigured = await CompanyService.IsEmailConfiguredAsync();
        if (!isEmailConfigured)
        {
            _errorMessage = "E-Mail-Versand ist nicht konfiguriert. Bitte richten Sie zuerst die E-Mail-Einstellungen ein.";
            return;
        }

        _saving = true;

        try
        {
            if (_selectedCustomer != null)
                _invoice.CustomerId = _selectedCustomer.Id;

            var invoiceDate = _invoiceDate ?? throw new InvalidOperationException("Rechnungsdatum fehlt.");
            _invoice.InvoiceDate = DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc);
            _invoice.ServicePeriodStart = _servicePeriodStart.HasValue ? DateTime.SpecifyKind(_servicePeriodStart.Value, DateTimeKind.Utc) : null;
            _invoice.ServicePeriodEnd   = _servicePeriodEnd.HasValue ? DateTime.SpecifyKind(_servicePeriodEnd.Value, DateTimeKind.Utc) : null;
            _invoice.DueDate            = _dueDate.HasValue ? DateTime.SpecifyKind(_dueDate.Value, DateTimeKind.Utc) : null;

            if (_attachTimesheet)
            {
                var ok = await EnsureTimesheetAttachmentAsync();
                if (!ok)
                {
                    _saving = false;
                    return;
                }
            }

            _invoice.Status = InvoiceStatus.Sent;

            var createdInvoice = await InvoiceService.CreateAsync(_invoice);

            Snackbar.Add($"Rechnung {_invoice.InvoiceNumber} wurde erfolgreich erstellt.", Severity.Success);

            // E-Mail-Dialog öffnen
            await OpenEmailDialog(createdInvoice);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
            _saving = false;
        }
    }

    private async Task OpenEmailDialog(Invoice invoice)
    {
        var parameters = new DialogParameters
        {
            [nameof(SendEmailDialog.Invoice)] = invoice,
            [nameof(SendEmailDialog.CustomerEmail)] = _selectedCustomer?.Email,
            [nameof(SendEmailDialog.OnSend)] =
                EventCallback.Factory.Create<(string Email, string? Message, EmailAttachmentFormat Format, string? CcEmails, string? BccEmails)>(
                    this, SendInvoiceEmail)
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SendEmailDialog>("E-Mail versenden", parameters, options);
        var result = await dialog.Result;

        // zurück zur Rechnungsliste
        _saving = false;
        NavigationManager.NavigateTo("/faktura/invoices");
    }

    private async Task SendInvoiceEmail((string Email, string? Message, EmailAttachmentFormat Format, string? CcEmails, string? BccEmails) data)
    {
        if (_invoice == null) return;

        try
        {
            var success = await EmailService.SendInvoiceEmailAsync(
                _invoice.Id,
                data.Email,
                data.Message,
                data.Format,
                data.CcEmails,
                data.BccEmails);

            if (success)
            {
                var formatText = data.Format switch
                {
                    EmailAttachmentFormat.ZugferdPdf => " als ZUGFeRD-PDF",
                    EmailAttachmentFormat.XRechnungXmlOnly => " als XRechnung-XML",
                    EmailAttachmentFormat.XRechnungXmlAndPdf => " als XRechnung (XML + PDF)",
                    _ => ""
                };

                Snackbar.Add(
                    $"Rechnung {_invoice.InvoiceNumber}{formatText} wurde erfolgreich an {data.Email} versendet.",
                    Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Versenden der E-Mail: {ex.Message}", Severity.Error);
            throw;
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/faktura/invoices");
    }

    private async Task HandleAttachmentUpload(IReadOnlyList<IBrowserFile> files)
    {
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

        if (_selectedCustomer == null)
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
        _invoice.Attachments.Remove(attachment);
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



