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

namespace Kuestencode.Faktura.Pages.Settings;

public partial class EmailCustomization
{
    private Company _company = new();
    private bool _loading = true;
    private bool _saving = false;
    private MudBlazor.Utilities.MudColor _primaryColorValue;
    private MudBlazor.Utilities.MudColor _accentColorValue;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _company = await CompanyService.GetCompanyAsync();
            _primaryColorValue = new MudBlazor.Utilities.MudColor(_company.EmailPrimaryColor);
            _accentColorValue = new MudBlazor.Utilities.MudColor(_company.EmailAccentColor);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Laden: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender)
        {
            _company.EmailPrimaryColor = _primaryColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
            _company.EmailAccentColor = _accentColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
        }
    }

    private void SetPrimaryColor(string color)
    {
        _primaryColorValue = new MudBlazor.Utilities.MudColor(color);
        _company.EmailPrimaryColor = color;
    }

    private void SetAccentColor(string color)
    {
        _accentColorValue = new MudBlazor.Utilities.MudColor(color);
        _company.EmailAccentColor = color;
    }

    private async Task HandleSubmit()
    {
        _saving = true;
        try
        {
            _company.EmailPrimaryColor = _primaryColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
            _company.EmailAccentColor = _accentColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);

            await CompanyService.UpdateCompanyAsync(_company);
            Snackbar.Add("E-Mail-Anpassungen gespeichert", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Speichern: {ex.Message}", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task ResetToDefaults()
    {
        var result = await DialogService.ShowMessageBox(
            "Auf Standard zurücksetzen",
            "Möchten Sie wirklich alle E-Mail-Anpassungen auf die Standardwerte zurücksetzen?",
            yesText: "Ja, zurücksetzen",
            cancelText: "Abbrechen");

        if (result == true)
        {
            _company.EmailLayout = EmailLayout.Klar;
            _company.EmailPrimaryColor = "#0F2A3D";
            _company.EmailAccentColor = "#3FA796";
            _company.EmailGreeting = null;
            _company.EmailClosing = null;

            _primaryColorValue = new MudBlazor.Utilities.MudColor(_company.EmailPrimaryColor);
            _accentColorValue = new MudBlazor.Utilities.MudColor(_company.EmailAccentColor);

            Snackbar.Add("Auf Standardwerte zurückgesetzt", Severity.Info);
        }
    }
}
