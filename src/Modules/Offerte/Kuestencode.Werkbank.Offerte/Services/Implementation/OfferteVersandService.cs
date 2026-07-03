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
    private readonly IEmailEngine _emailEngine;
    private readonly ICustomerService _customerService;
    private readonly ICompanyService _companyService;
    private readonly IOfferteSettingsService _settingsService;
    private readonly IOfferteEmailTemplateRenderer _templateRenderer;
    private readonly AngebotStatusService _statusService;
    private readonly ILogger<OfferteVersandService> _logger;

    public OfferteVersandService(
        IAngebotRepository repository,
        IOffertePdfService pdfService,
        IEmailEngine emailEngine,
        ICustomerService customerService,
        ICompanyService companyService,
        IOfferteSettingsService settingsService,
        IOfferteEmailTemplateRenderer templateRenderer,
        AngebotStatusService statusService,
        ILogger<OfferteVersandService> logger)
    {
        _repository = repository;
        _pdfService = pdfService;
        _emailEngine = emailEngine;
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
        string? nachricht = null,
        string? ccEmails = null,
        string? bccEmails = null,
        bool includeClosing = true)
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

        var firmenName = firma.BusinessName ?? firma.OwnerFullName;
        var emailBetreff = betreff ?? $"Angebot {angebot.Angebotsnummer} von {firmenName}";
        var greeting = _templateRenderer.ResolveGreeting(kunde, nachricht);
        var contentHtml = _templateRenderer.RenderContentHtml(angebot);
        var contentText = _templateRenderer.RenderContentText(angebot);

        try
        {
            // E-Mail senden
            var attachment = new EmailAttachment
            {
                FileName = $"Angebot_{angebot.Angebotsnummer}.pdf",
                Content = pdfBytes,
                ContentType = "application/pdf"
            };

            await _emailEngine.SendEmailAsync(
                recipientEmail: email,
                subject: emailBetreff,
                contentHtml: contentHtml,
                contentText: contentText,
                attachments: new[] { attachment },
                ccEmails: ccEmails,
                bccEmails: bccEmails,
                greeting: greeting,
                includeClosing: includeClosing);

            // Status aktualisieren
            _statusService.Versenden(angebot);
            angebot.EmailGesendetAm = DateTime.UtcNow;
            angebot.EmailGesendetAn = email;
            angebot.EmailAnzahl++;

            await _repository.UpdateAsync(angebot);

            _logger.LogInformation(
                "Angebot {Angebotsnummer} erfolgreich versendet an {Email}",
                angebot.Angebotsnummer, email);

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
