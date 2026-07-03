using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Models;
using Kuestencode.Core.Services;
using Kuestencode.Shared.UI.Components.Settings;

namespace Kuestencode.Werkbank.Host.Pages.Settings;

public partial class EmailDesign
{
    private Company _company = new();
    private EmailSettingsModel _model = new();
    private bool _loading = true;
    private bool _saving = false;
    private List<PreviewTab> _previewTabs = new();

    private record PreviewTab(string Title, string Html);

    private static readonly Dictionary<string, (string Title, string ContentHtml)> ModuleExamples = new()
    {
        ["Faktura"] = ("Rechnung", FakturaExampleHtml),
        ["Offerte"] = ("Angebot", OfferteExampleHtml),
        ["Rapport"] = ("Tätigkeitsnachweis", RapportExampleHtml)
    };

    private const string FakturaExampleHtml =
        "<p>anbei erhalten Sie die Rechnung <strong>RE-2025-001</strong>.</p>" +
        "<table style='width:100%; border-collapse:collapse; background-color:white; padding:15px; margin:15px 0;'>" +
        "<tr><td><strong>Rechnungsbetrag:</strong></td><td><strong>1.190,00 €</strong></td></tr>" +
        "<tr><td><strong>Rechnungsnummer:</strong></td><td>RE-2025-001</td></tr>" +
        "<tr><td><strong>Rechnungsdatum:</strong></td><td>17.01.2025</td></tr>" +
        "<tr><td><strong>Fällig am:</strong></td><td>31.01.2025</td></tr>" +
        "</table>" +
        "<p>Die Rechnung finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>";

    private const string OfferteExampleHtml =
        "<p>anbei erhalten Sie unser Angebot <strong>AN-2025-001</strong>.</p>" +
        "<table style='width:100%; border-collapse:collapse; background-color:white; padding:15px; margin:15px 0;'>" +
        "<tr><td><strong>Angebotsbetrag:</strong></td><td><strong>3.925,00 €</strong></td></tr>" +
        "<tr><td><strong>Angebotsnummer:</strong></td><td>AN-2025-001</td></tr>" +
        "<tr><td><strong>Angebotsdatum:</strong></td><td>17.01.2025</td></tr>" +
        "<tr><td><strong>Gültig bis:</strong></td><td>15.02.2025</td></tr>" +
        "</table>" +
        "<p>Das Angebot finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>";

    private const string RapportExampleHtml =
        "<p>anbei erhalten Sie den Tätigkeitsnachweis für <strong>Musterkunde GmbH</strong> im Zeitraum <strong>01.01.2025 - 31.01.2025</strong>.</p>" +
        "<table style='width:100%; border-collapse:collapse; background-color:white; padding:15px; margin:15px 0;'>" +
        "<tr><td><strong>Kunde:</strong></td><td>Musterkunde GmbH</td></tr>" +
        "<tr><td><strong>Zeitraum:</strong></td><td>01.01.2025 - 31.01.2025</td></tr>" +
        "<tr><td><strong>Gesamtstunden:</strong></td><td>42,5 Std.</td></tr>" +
        "</table>" +
        "<p>Den Tätigkeitsnachweis finden Sie im Anhang dieser E-Mail als PDF-Datei.</p>";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _company = await CompanyService.GetCompanyAsync();
            _model = FromCompany(_company);
            RebuildPreviews();
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

    private void OnSettingsChanged(EmailSettingsModel model)
    {
        ApplyToCompany(_company, model);
        RebuildPreviews();
    }

    private async Task HandleSubmit()
    {
        _saving = true;
        try
        {
            ApplyToCompany(_company, _model);
            await CompanyService.UpdateCompanyAsync(_company);
            Snackbar.Add("E-Mail-Design gespeichert", Severity.Success);
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
            "Möchten Sie wirklich alle E-Mail-Design-Einstellungen auf die Standardwerte zurücksetzen?",
            yesText: "Ja, zurücksetzen",
            cancelText: "Abbrechen");

        if (result == true)
        {
            _model = new EmailSettingsModel();
            ApplyToCompany(_company, _model);
            RebuildPreviews();
            Snackbar.Add("Auf Standardwerte zurückgesetzt", Severity.Info);
        }
    }

    private void RebuildPreviews()
    {
        _previewTabs = ModuleRegistry.GetAllModules()
            .Where(m => ModuleExamples.ContainsKey(m.ModuleName))
            .Select(m =>
            {
                var (title, contentHtml) = ModuleExamples[m.ModuleName];
                var html = EmailTemplateRenderer.WrapHtml(_company, contentHtml, greeting: null, includeClosing: true);
                return new PreviewTab(title, html);
            })
            .ToList();
    }

    private static EmailSettingsModel FromCompany(Company company) => new()
    {
        Layout = company.EmailLayout,
        PrimaryColor = company.EmailPrimaryColor,
        AccentColor = company.EmailAccentColor,
        Greeting = company.EmailGreeting,
        Closing = company.EmailClosing,
        Signature = company.EmailSignature
    };

    private static void ApplyToCompany(Company company, EmailSettingsModel model)
    {
        company.EmailLayout = model.Layout;
        company.EmailPrimaryColor = model.PrimaryColor;
        company.EmailAccentColor = model.AccentColor;
        company.EmailGreeting = model.Greeting;
        company.EmailClosing = model.Closing;
        company.EmailSignature = model.Signature;
    }
}
