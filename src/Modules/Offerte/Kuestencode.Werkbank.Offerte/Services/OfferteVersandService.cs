using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Offerte.Data.Repositories;
using Kuestencode.Werkbank.Offerte.Domain.Services;
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
    private readonly AngebotStatusService _statusService;
    private readonly ILogger<OfferteVersandService> _logger;

    public OfferteVersandService(
        IAngebotRepository repository,
        IOffertePdfService pdfService,
        IEmailService emailService,
        ICustomerService customerService,
        ICompanyService companyService,
        AngebotStatusService statusService,
        ILogger<OfferteVersandService> logger)
    {
        _repository = repository;
        _pdfService = pdfService;
        _emailService = emailService;
        _customerService = customerService;
        _companyService = companyService;
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

        // E-Mail-Adresse bestimmen
        var email = empfaengerEmail ?? kunde.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Keine E-Mail-Adresse für den Kunden hinterlegt.");
        }

        // PDF erzeugen
        var pdfBytes = _pdfService.Erstelle(angebot, kunde, firma);

        // E-Mail-Templates
        var firmenName = firma.BusinessName ?? firma.OwnerFullName;
        var emailBetreff = betreff ?? $"Angebot {angebot.Angebotsnummer} von {firmenName}";
        var emailBody = nachricht ?? ErstelleStandardNachricht(angebot, kunde, firma);

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
                htmlBody: emailBody,
                plainTextBody: emailBody,
                attachments: new[] { attachment });

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

    private string ErstelleStandardNachricht(
        Domain.Entities.Angebot angebot,
        Customer kunde,
        Company firma)
    {
        var anrede = !string.IsNullOrEmpty(kunde.Salutation)
            ? kunde.Salutation
            : (firma.EmailGreeting ?? "Sehr geehrte Damen und Herren,");

        var firmenName = firma.BusinessName ?? firma.OwnerFullName;
        var gruss = firma.EmailClosing ?? "Mit freundlichen Grüßen";

        return $@"{anrede}

anbei erhalten Sie unser Angebot {angebot.Angebotsnummer} vom {angebot.Erstelldatum:dd.MM.yyyy}.

Das Angebot ist gültig bis zum {angebot.GueltigBis:dd.MM.yyyy}.

Bei Fragen stehen wir Ihnen gerne zur Verfügung.

{gruss}
{firmenName}";
    }
}
