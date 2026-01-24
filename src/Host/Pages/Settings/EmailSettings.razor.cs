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

public partial class EmailSettings
{
    private Company _company = new();
    private EmailSettingsModel _model = new();
    private bool _loading = true;
    private bool _saving = false;
    private bool _testing = false;
    private bool _helpExpanded = false;
    private bool _hasStoredPassword = false;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _company = await CompanyService.GetCompanyAsync();
            _hasStoredPassword = !string.IsNullOrWhiteSpace(_company.SmtpPassword);
            _model = EmailSettingsModel.FromCompany(_company);
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
            if (!_hasStoredPassword && string.IsNullOrWhiteSpace(_model.SmtpPassword))
            {
                _errorMessage = "Passwort ist erforderlich";
                return;
            }

            ApplyToCompany(_company, _model);
            await CompanyService.UpdateCompanyAsync(_company);
            if (!string.IsNullOrWhiteSpace(_model.SmtpPassword))
            {
                _hasStoredPassword = true;
                _model.SmtpPassword = string.Empty;
            }
            Snackbar.Add("Email Settings gespeichert", Severity.Success);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Speichern: {ex.Message}";
            Snackbar.Add("Fehler beim Speichern der Email Settings", Severity.Error);
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
            _hasStoredPassword = !string.IsNullOrWhiteSpace(_company.SmtpPassword);
            _model = EmailSettingsModel.FromCompany(_company);
            Snackbar.Add("Formular zurueckgesetzt", Severity.Info);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Fehler beim Zuruecksetzen: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task TestConnection()
    {
        _testing = true;
        _errorMessage = null;

        try
        {
            var (success, errorMessage) = await EmailEngine.TestConnectionAsync();
            if (success)
            {
                Snackbar.Add("SMTP Verbindung erfolgreich getestet.", Severity.Success);
            }
            else
            {
                Snackbar.Add(errorMessage ?? "SMTP Verbindung fehlgeschlagen.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Fehler beim Verbindungstest: {ex.Message}", Severity.Error);
        }
        finally
        {
            _testing = false;
        }
    }

    private static void ApplyToCompany(Company company, EmailSettingsModel model)
    {
        company.SmtpHost = model.SmtpHost;
        company.SmtpPort = model.SmtpPort;
        company.SmtpUseSsl = model.SmtpUseSsl;
        company.SmtpUsername = model.SmtpUsername;
        if (!string.IsNullOrWhiteSpace(model.SmtpPassword))
        {
            company.SmtpPassword = model.SmtpPassword;
        }
        company.EmailSenderEmail = model.SenderEmail;
        company.EmailSenderName = model.SenderName;
        company.EmailSignature = model.Signature;
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_model.SmtpHost) &&
               _model.SmtpPort.HasValue &&
               !string.IsNullOrWhiteSpace(_model.SmtpUsername) &&
               (_hasStoredPassword || !string.IsNullOrWhiteSpace(_model.SmtpPassword)) &&
               !string.IsNullOrWhiteSpace(_model.SenderEmail);
    }

    private class EmailSettingsModel
    {
        [Required(ErrorMessage = "SMTP Host ist erforderlich")]
        [MaxLength(200)]
        public string SmtpHost { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Port ist erforderlich")]
        [Range(1, 65535, ErrorMessage = "Port muss zwischen 1 und 65535 liegen")]
        public int? SmtpPort { get; set; } = 587;

        public bool SmtpUseSsl { get; set; } = true;

        [Required(ErrorMessage = "Benutzername ist erforderlich")]
        [MaxLength(200)]
        public string SmtpUsername { get; set; } = string.Empty;

        [MaxLength(500)]
        public string SmtpPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Absender Email ist erforderlich")]
        [EmailAddress(ErrorMessage = "Ungueltige Email-Adresse")]
        [MaxLength(200)]
        public string SenderEmail { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? SenderName { get; set; }

        [MaxLength(2000)]
        public string? Signature { get; set; }

        public static EmailSettingsModel FromCompany(Company company)
        {
            return new EmailSettingsModel
            {
                SmtpHost = company.SmtpHost ?? string.Empty,
                SmtpPort = company.SmtpPort ?? 587,
                SmtpUseSsl = company.SmtpUseSsl,
                SmtpUsername = company.SmtpUsername ?? string.Empty,
                SmtpPassword = string.Empty,
                SenderEmail = company.EmailSenderEmail ?? string.Empty,
                SenderName = company.EmailSenderName,
                Signature = company.EmailSignature
            };
        }
    }
}
