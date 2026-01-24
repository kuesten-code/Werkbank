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

public partial class PdfCustomization
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
            _primaryColorValue = new MudBlazor.Utilities.MudColor(_company.PdfPrimaryColor);
            _accentColorValue = new MudBlazor.Utilities.MudColor(_company.PdfAccentColor);
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
            _company.PdfPrimaryColor = _primaryColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
            _company.PdfAccentColor = _accentColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
        }
    }

    private void SetPrimaryColor(string color)
    {
        _primaryColorValue = new MudBlazor.Utilities.MudColor(color);
        _company.PdfPrimaryColor = color;
    }

    private void SetAccentColor(string color)
    {
        _accentColorValue = new MudBlazor.Utilities.MudColor(color);
        _company.PdfAccentColor = color;
    }

    private async Task HandleSubmit()
    {
        _saving = true;
        try
        {
            _company.PdfPrimaryColor = _primaryColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);
            _company.PdfAccentColor = _accentColorValue.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex);

            await CompanyService.UpdateCompanyAsync(_company);
            Snackbar.Add("PDF-Anpassungen gespeichert", Severity.Success);
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
            "Möchten Sie wirklich alle PDF-Anpassungen auf die Standardwerte zurücksetzen?",
            yesText: "Ja, zurücksetzen",
            cancelText: "Abbrechen");

        if (result == true)
        {
            _company.PdfLayout = PdfLayout.Klar;
            _company.PdfPrimaryColor = "#1f3a5f";
            _company.PdfAccentColor = "#3FA796";
            _company.PdfHeaderText = null;
            _company.PdfFooterText = null;
            _company.PdfPaymentNotice = null;

            _primaryColorValue = new MudBlazor.Utilities.MudColor(_company.PdfPrimaryColor);
            _accentColorValue = new MudBlazor.Utilities.MudColor(_company.PdfAccentColor);

            Snackbar.Add("Auf Standardwerte zurückgesetzt", Severity.Info);
        }
    }
}
