using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Pages.Customers;

public partial class Create
{
    private Customer _customer = new() { Country = "Deutschland" };
    private bool _saving = false;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _customer.CustomerNumber = await CustomerService.GenerateCustomerNumberAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Generieren der Kundennummer: {ex.Message}";
        }
    }

    private async Task HandleSubmit()
    {
        _saving = true;
        _errorMessage = null;

        try
        {
            await CustomerService.CreateAsync(_customer);
            Snackbar.Add($"Kunde '{_customer.Name}' wurde erfolgreich erstellt.", Severity.Success);
            NavigationManager.NavigateTo("/customers");
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
        NavigationManager.NavigateTo("/customers");
    }
}
