using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Services;
using Kuestencode.Werkbank.Offerte.Services.Email;
using Kuestencode.Werkbank.Offerte.Services.Pdf;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Service zum Versenden von Angeboten per E-Mail.
/// </summary>
public class OfferteVersandService : IOfferteVersandService
{
    private readonly IAngebotRepository _repository;
    private readonly IOffertePdfService _pdfService;
    private readonly IEmailService _emailService;
    private readonly ICustomerService _customerService;
    private readonly ICompanyService _companyService;
    private readonly IOfferteSettingsService _settingsService;
    private readonly IOfferteEmailTemplateRenderer _templateRenderer;
    private readonly AngebotStatusService _statusService;
    private readonly ILogger<OfferteVersandService> _logger;

    public OfferteVersandService(
        IAngebotRepository repository,
        IOffertePdfService pdfService,
        IEmailService emailService,
        ICustomerService customerService,
        ICompanyService companyService,
        IOfferteSettingsService settingsService,
        IOfferteEmailTemplateRenderer templateRenderer,
        AngebotStatusService statusService,
        ILogger<OfferteVersandService> logger)
    {
        _repository = repository;
        _pdfService = pdfService;
        _emailService = emailService;
        _customerService = customerService;
        _companyService = companyService;
        _settingsService = settingsService;
        _templateRenderer = templateRenderer;
        _statusService = statusService;
        _logger = logger;
    }

    public async Task<bool> VersendeAsync(
        Guid angebotId,
        string? empfaengerEmail = null,
        string? betreff = null,
        string? nachricht = null)
    {
        var angebot = await _repository.GetByIdAsync(angebotId);
        if (angebot == null)
        {
            throw new InvalidOperationException($"Angebot mit ID {angebotId} nicht gefunden.");
        }

        if (!_statusService.KannVersendetWerden(angebot))
        {
            throw new InvalidOperationException(
                $"Angebot kann nicht versendet werden. Aktueller Status: {angebot.Status}");
        }

        var kunde = await _customerService.GetByIdAsync(angebot.KundeId);
        if (kunde == null)
        {
            throw new InvalidOperationException($"Kunde mit ID {angebot.KundeId} nicht gefunden.");
        }

        var firma = await _companyService.GetCompanyAsync();
        var settings = await _settingsService.GetSettingsAsync();

        // E-Mail-Adresse bestimmen
        var email = empfaengerEmail ?? kunde.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Keine E-Mail-Adresse für den Kunden hinterlegt.");
        }

        // PDF erzeugen
        var pdfBytes = _pdfService.Erstelle(angebot, kunde, firma, settings);

        // E-Mail-Templates mit Layout-Unterstützung
        var firmenName = firma.BusinessName ?? firma.OwnerFullName;
        var emailBetreff = betreff ?? $"Angebot {angebot.Angebotsnummer} von {firmenName}";

        // Wenn eine benutzerdefinierte Nachricht angegeben wurde, verwende sie;
        // sonst rendere HTML und Plain-Text mit dem Template-Renderer
        string htmlBody;
        string plainTextBody;

        if (!string.IsNullOrWhiteSpace(nachricht))
        {
            // Benutzerdefinierte Nachricht - verwende sie als Greeting im Template
            htmlBody = _templateRenderer.RenderHtmlBody(angebot, kunde, firma, settings, nachricht);
            plainTextBody = _templateRenderer.RenderPlainTextBody(angebot, kunde, firma, settings, nachricht);
        }
        else
        {
            // Standard-Template verwenden
            htmlBody = _templateRenderer.RenderHtmlBody(angebot, kunde, firma, settings);
            plainTextBody = _templateRenderer.RenderPlainTextBody(angebot, kunde, firma, settings);
        }

        try
        {
            // E-Mail senden
            var attachment = new EmailAttachment
            {
                FileName = $"Angebot_{angebot.Angebotsnummer}.pdf",
                Content = pdfBytes,
                ContentType = "application/pdf"
            };

            await _emailService.SendEmailAsync(
                recipientEmail: email,
                subject: emailBetreff,
                htmlBody: htmlBody,
                plainTextBody: plainTextBody,
                attachments: new[] { attachment });

            // Status aktualisieren
            _statusService.Versenden(angebot);
            angebot.EmailGesendetAm = DateTime.UtcNow;
            angebot.EmailGesendetAn = email;
            angebot.EmailAnzahl++;

            await _repository.UpdateAsync(angebot);

            _logger.LogInformation(
                "Angebot {Angebotsnummer} erfolgreich versendet an {Email} mit Layout {Layout}",
                angebot.Angebotsnummer, email, settings.EmailLayout);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Fehler beim Versenden von Angebot {Angebotsnummer} an {Email}",
                angebot.Angebotsnummer, email);
            throw;
        }
    }
}
