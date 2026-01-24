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

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class CompanySettings
{
    private Company _company = new();
    private bool _loading = true;
    private bool _saving = false;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _company = await CompanyService.GetCompanyAsync();
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

    private async Task HandleSubmit()
    {
        _saving = true;
        _errorMessage = null;

        try
        {
            await CompanyService.UpdateCompanyAsync(_company);
            Snackbar.Add("Firmendaten gespeichert", Severity.Success);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
            Snackbar.Add("Fehler beim Speichern der Firmendaten", Severity.Error);
        }
        finally
        {
            _saving = false;
        }
    }

    private async Task ResetForm()
    {
        _loading = true;
        try
        {
            _company = await CompanyService.GetCompanyAsync();
            Snackbar.Add("Formular zurückgesetzt", Severity.Info);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Zurücksetzen: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleLogoUpload(IBrowserFile file)
    {
        // Validierung: Dateigröße
        if (file.Size > 2 * 1024 * 1024) // 2 MB
        {
            Snackbar.Add("Logo darf maximal 2 MB groß sein", Severity.Error);
            return;
        }

        // Validierung: Dateityp
        var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            Snackbar.Add("Nur PNG und JPG Dateien erlaubt", Severity.Error);
            return;
        }

        try
        {
            // Read into memory instead of filesystem
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024).CopyToAsync(memoryStream);

            // Store in database
            _company.LogoData = memoryStream.ToArray();
            _company.LogoContentType = file.ContentType;

            // Save to database
            await CompanyService.UpdateCompanyAsync(_company);

            Snackbar.Add("Logo erfolgreich hochgeladen", Severity.Success);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Upload: {ex.Message}", Severity.Error);
        }
    }

    private async Task RemoveLogo()
    {
        try
        {
            _company.LogoData = null;
            _company.LogoContentType = null;

            await CompanyService.UpdateCompanyAsync(_company);

            Snackbar.Add("Logo entfernt", Severity.Success);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Entfernen: {ex.Message}", Severity.Error);
        }
    }

    private string GetLogoDataUrl()
    {
        if (_company.LogoData == null || _company.LogoData.Length == 0)
            return string.Empty;

        var base64 = Convert.ToBase64String(_company.LogoData);
        return $"data:{_company.LogoContentType};base64,{base64}";
    }
}
