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

public partial class Edit
{
    [Parameter]
    public int Id { get; set; }

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
}
