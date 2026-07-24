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

public partial class Create
{
    [Inject]
    public ModuleAvailabilityService AvailabilityService { get; set; } = null!;

    [Inject]
    public IActaApiClient ActaApiClient { get; set; } = null!;

    private bool _customerError;
    private string? _customerErrorText;
    private MudAutocomplete<Customer>? _customerAuto;
    private Invoice _invoice = new() { Type = InvoiceType.CreditNote };
    private string _creditNoteNumberPrefix = string.Empty;
    private string _creditNoteNumberSuffix = string.Empty;
    private string _creditNoteNumberSequence = string.Empty;
    private Customer? _selectedCustomer;
    private Invoice? _relatedInvoice;
    private List<Invoice> _relatedInvoiceCandidates = new();
    private Company? _company;
    private List<Customer> _customers = new();
    private DateTime? _invoiceDate = DateTime.Today;
    private DateTime? _servicePeriodStart;
    private DateTime? _servicePeriodEnd;
    private DateTime? _dueDate;
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
            var (prefix, suffix, _) = await InvoiceService.GetCreditNoteNumberFormatPartsAsync();
            _creditNoteNumberPrefix = prefix;
            _creditNoteNumberSuffix = suffix;

            _invoice.InvoiceNumber = await InvoiceService.GenerateCreditNoteNumberAsync();
            _creditNoteNumberSequence = _invoice.InvoiceNumber.Substring(
                _creditNoteNumberPrefix.Length,
                _invoice.InvoiceNumber.Length - _creditNoteNumberPrefix.Length - _creditNoteNumberSuffix.Length);

            _customers = await CustomerService.GetAllAsync();
            _company = await CompanyService.GetCompanyAsync();
            var paymentDays = _company?.DefaultPaymentTermDays ?? 14;
            _dueDate = DateTime.Today.AddDays(paymentDays);

            _relatedInvoiceCandidates = await InvoiceService.GetByTypeAsync(InvoiceType.Invoice);

            AddItem();
            RecalculateTotals();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Initialisieren: {ex.Message}";
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

    private void OnRelatedInvoiceChanged()
    {
        if (_relatedInvoice == null)
        {
            return;
        }

        _selectedCustomer = _customers.FirstOrDefault(c => c.Id == _relatedInvoice.CustomerId);
    }

    private void UpdateCreditNoteNumber()
    {
        _invoice.InvoiceNumber = _creditNoteNumberPrefix + _creditNoteNumberSequence + _creditNoteNumberSuffix;
    }

    private string? ValidateCreditNoteNumberSequence(string sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return "Laufende Nummer ist erforderlich";
        }

        return sequence.All(char.IsDigit) ? null : "Nur Ziffern erlaubt";
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

    private async Task<IEnumerable<Invoice>> SearchRelatedInvoices(string value, CancellationToken token)
    {
        await Task.CompletedTask; // Async compatibility

        if (string.IsNullOrWhiteSpace(value))
            return _relatedInvoiceCandidates;

        return _relatedInvoiceCandidates.Where(i =>
            i.InvoiceNumber.Contains(value, StringComparison.OrdinalIgnoreCase));
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

        foreach (var item in _invoice.Items)
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
            VatRate = vatRate
        });
        ReorderItems();
    }

    private void AddHeader()
    {
        _invoice.Items.Add(new InvoiceItem
        {
            IsHeader = true,
            Description = string.Empty,
            Quantity = 0,
            UnitPrice = 0,
            VatRate = 0
        });
        ReorderItems();
    }

    private void RemoveItem(InvoiceItem item)
    {
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
        for (int i = 0; i < _invoice.Items.Count; i++)
        {
            _invoice.Items[i].Position = i + 1;
        }
    }

    private void OnReverseChargeToggle(bool value)
    {
        _isReverseCharge = value;
        _invoice.IsReverseCharge = value;
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        // Ensure all items have the correct VAT rate based on company settings
        decimal vatRate;
        if (_company?.IsKleinunternehmer == true)
            vatRate = 0;
        else if (_isReverseCharge)
            vatRate = 0;
        else
            vatRate = 19;
        foreach (var item in _invoice.Items.Where(i => !i.IsHeader))
        {
            item.VatRate = vatRate;
        }

        _totalNet = _invoice.TotalNet;
        _totalVat = _invoice.TotalVat;
        _totalGross = _invoice.TotalGross;

        StateHasChanged();
    }

    private async Task<bool> ValidateCreditNoteAsync()
    {
        _customerError = false;
        _customerErrorText = null;
        _errorMessage = null;

        if (ValidateCreditNoteNumberSequence(_creditNoteNumberSequence) != null)
        {
            _errorMessage = "Gutschriftnummer: Laufende Nummer darf nur Ziffern enthalten.";
            return false;
        }

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
        var nonHeaderItems = _invoice.Items.Where(i => !i.IsHeader).ToList();
        if (nonHeaderItems.Count == 0 || nonHeaderItems.All(i => string.IsNullOrWhiteSpace(i.Description)))
        {
            _errorMessage = "Mindestens eine Position angeben.";
            return false;
        }

        if (nonHeaderItems.Any(i => i.Quantity <= 0))
        {
            _errorMessage = "Menge muss > 0 sein.";
            return false;
        }

        if (nonHeaderItems.Any(i => i.UnitPrice <= 0))
        {
            _errorMessage = "Einzelpreis muss größer als 0 sein.";
            return false;
        }

        // Gutschriftdatum: Pflicht
        if (_invoiceDate == null)
        {
            _errorMessage = "Gutschriftdatum fehlt.";
            return false;
        }

        if (_invoiceDate.Value > DateTime.Today)
        {
            _errorMessage = "Gutschriftdatum darf nicht in der Zukunft liegen.";
            return false;
        }

        return true;
    }

    private void ApplySignConvention()
    {
        foreach (var item in _invoice.Items.Where(i => !i.IsHeader))
        {
            item.UnitPrice = -Math.Abs(item.UnitPrice);
        }
    }

    private async Task SaveAsync(bool asDraft)
    {
        if (!await ValidateCreditNoteAsync())
            return;

        _saving = true;

        try
        {
            if (_selectedCustomer != null)
                _invoice.CustomerId = _selectedCustomer.Id;

            _invoice.ProjectId = _selectedProjectExternalId;
            _invoice.RelatedInvoiceId = _relatedInvoice?.Id;

            var invoiceDate = _invoiceDate ?? throw new InvalidOperationException("Gutschriftdatum fehlt.");
            _invoice.InvoiceDate = DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc);
            _invoice.ServicePeriodStart = _servicePeriodStart.HasValue ? DateTime.SpecifyKind(_servicePeriodStart.Value, DateTimeKind.Utc) : null;
            _invoice.ServicePeriodEnd   = _servicePeriodEnd.HasValue ? DateTime.SpecifyKind(_servicePeriodEnd.Value, DateTimeKind.Utc) : null;
            _invoice.DueDate            = _dueDate.HasValue ? DateTime.SpecifyKind(_dueDate.Value, DateTimeKind.Utc) : null;

            _invoice.Status = asDraft ? InvoiceStatus.Draft : InvoiceStatus.Sent;

            ApplySignConvention();

            await InvoiceService.CreateAsync(_invoice);

            Snackbar.Add($"Gutschrift {_invoice.InvoiceNumber} wurde erfolgreich erstellt.",
                Severity.Success);

            NavigationManager.NavigateTo("/faktura/credit-notes");
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
        if (!await ValidateCreditNoteAsync())
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

            _invoice.ProjectId = _selectedProjectExternalId;
            _invoice.RelatedInvoiceId = _relatedInvoice?.Id;

            var invoiceDate = _invoiceDate ?? throw new InvalidOperationException("Gutschriftdatum fehlt.");
            _invoice.InvoiceDate = DateTime.SpecifyKind(invoiceDate, DateTimeKind.Utc);
            _invoice.ServicePeriodStart = _servicePeriodStart.HasValue ? DateTime.SpecifyKind(_servicePeriodStart.Value, DateTimeKind.Utc) : null;
            _invoice.ServicePeriodEnd   = _servicePeriodEnd.HasValue ? DateTime.SpecifyKind(_servicePeriodEnd.Value, DateTimeKind.Utc) : null;
            _invoice.DueDate            = _dueDate.HasValue ? DateTime.SpecifyKind(_dueDate.Value, DateTimeKind.Utc) : null;

            _invoice.Status = InvoiceStatus.Sent;

            ApplySignConvention();

            var createdCreditNote = await InvoiceService.CreateAsync(_invoice);

            Snackbar.Add($"Gutschrift {_invoice.InvoiceNumber} wurde erfolgreich erstellt.", Severity.Success);

            // E-Mail-Dialog öffnen
            await OpenEmailDialog(createdCreditNote);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
            _saving = false;
        }
    }

    private async Task OpenEmailDialog(Invoice creditNote)
    {
        var parameters = new DialogParameters
        {
            [nameof(SendEmailDialog.Invoice)] = creditNote,
            [nameof(SendEmailDialog.CustomerEmail)] = _selectedCustomer?.Email,
            [nameof(SendEmailDialog.OnSend)] =
                EventCallback.Factory.Create<(string Email, string? Message, EmailAttachmentFormat Format, string? CcEmails, string? BccEmails, bool IncludeClosing)>(
                    this, SendCreditNoteEmail)
        };

        var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
        var dialog = await DialogService.ShowAsync<SendEmailDialog>("E-Mail versenden", parameters, options);
        var result = await dialog.Result;

        // zurück zur Gutschriftenliste
        _saving = false;
        NavigationManager.NavigateTo("/faktura/credit-notes");
    }

    private async Task SendCreditNoteEmail((string Email, string? Message, EmailAttachmentFormat Format, string? CcEmails, string? BccEmails, bool IncludeClosing) data)
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
                data.BccEmails,
                data.IncludeClosing);

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
                    $"Gutschrift {_invoice.InvoiceNumber}{formatText} wurde erfolgreich an {data.Email} versendet.",
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
        NavigationManager.NavigateTo("/faktura/credit-notes");
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
}
