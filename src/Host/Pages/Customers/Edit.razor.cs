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

public partial class Edit
{
    [Parameter]
    public int Id { get; set; }

    private Customer? _customer;
    private bool _loading = true;
    private bool _saving = false;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomer();
    }

    private async Task LoadCustomer()
    {
        _loading = true;
        try
        {
            _customer = await CustomerService.GetByIdAsync(Id);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Laden des Kunden: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleSubmit()
    {
        if (_customer == null) return;

        _saving = true;
        _errorMessage = null;

        try
        {
            await CustomerService.UpdateAsync(_customer);
            Snackbar.Add($"Kunde '{_customer.Name}' wurde erfolgreich aktualisiert.", Severity.Success);
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

    private async Task DeleteCustomer()
    {
        if (_customer == null) return;

        bool? result = await DialogService.ShowMessageBox(
            "Kunde löschen",
            $"Möchten Sie den Kunden '{_customer.Name}' wirklich löschen?",
            yesText: "Löschen",
            cancelText: "Abbrechen");

        if (result == true)
        {
            try
            {
                await CustomerService.DeleteAsync(_customer.Id);
                Snackbar.Add($"Kunde '{_customer.Name}' wurde gelöscht.", Severity.Success);
                NavigationManager.NavigateTo("/customers");
            }
            catch (Exception ex)
            {
                _errorMessage = $"Fehler beim Löschen: {ex.Message}";
            }
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/customers");
    }
}
