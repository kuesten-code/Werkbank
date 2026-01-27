using System.ComponentModel.DataAnnotations;
using System.Text;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Rapport.Models.Timesheets;
using Kuestencode.Shared.Contracts.Rapport;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Sends timesheet exports via email.
/// </summary>
public class TimesheetEmailService
{
    private readonly ICompanyService _companyService;
    private readonly TimesheetPdfService _pdfService;
    private readonly TimesheetCsvService _csvService;
    private readonly TimesheetExportService _exportService;
    private readonly ILogger<TimesheetEmailService> _logger;

    public TimesheetEmailService(
        ICompanyService companyService,
        TimesheetPdfService pdfService,
        TimesheetCsvService csvService,
        TimesheetExportService exportService,
        ILogger<TimesheetEmailService> logger)
    {
        _companyService = companyService;
        _pdfService = pdfService;
        _csvService = csvService;
        _exportService = exportService;
        _logger = logger;
    }

    public async Task SendAsync(TimesheetEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            throw new ValidationException("Empfänger-E-Mail-Adresse ist erforderlich.");
        }

        var company = await _companyService.GetCompanyAsync();
        if (string.IsNullOrWhiteSpace(company.SmtpHost) ||
            !company.SmtpPort.HasValue ||
            company.SmtpPort.Value <= 0 ||
            string.IsNullOrWhiteSpace(company.SmtpUsername))
        {
            throw new InvalidOperationException("E-Mail-Versand ist nicht konfiguriert.");
        }

        var exportRequest = new TimesheetExportRequestDto
        {
            CustomerId = request.CustomerId,
            From = request.From,
            To = request.To,
            ProjectId = request.ProjectId,
            HourlyRate = request.HourlyRate,
            Title = request.Title,
            FileName = request.FileName,
            EntryIds = request.EntryIds
        };

        var timesheet = await _exportService.BuildAsync(exportRequest);

        byte[] attachmentBytes;
        string attachmentName;
        string contentType;

        if (request.Format == TimesheetAttachmentFormat.Csv)
        {
            var result = await _csvService.GenerateAsync(exportRequest);
            attachmentBytes = result.Bytes;
            attachmentName = result.FileName;
            contentType = "text/csv";
        }
        else
        {
            var result = await _pdfService.GenerateAsync(exportRequest);
            attachmentBytes = result.Bytes;
            attachmentName = result.FileName;
            contentType = "application/pdf";
        }

        var message = BuildMessage(company, timesheet, request, attachmentBytes, attachmentName, contentType);

        using var smtp = new SmtpClient();
        try
        {
            var secureOptions = company.SmtpUseSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await smtp.ConnectAsync(company.SmtpHost, company.SmtpPort!.Value, secureOptions);

            if (!string.IsNullOrWhiteSpace(company.SmtpUsername) && !string.IsNullOrWhiteSpace(company.SmtpPassword))
            {
                await smtp.AuthenticateAsync(company.SmtpUsername, company.SmtpPassword);
            }

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Timesheet email sent to {Recipient}", request.RecipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timesheet email send failed");
            throw;
        }
    }

    private static MimeMessage BuildMessage(
        Company company,
        TimesheetDto timesheet,
        TimesheetEmailRequest request,
        byte[] attachmentBytes,
        string attachmentName,
        string contentType)
    {
        var message = new MimeMessage();

        var senderName = !string.IsNullOrWhiteSpace(company.EmailSenderName)
            ? company.EmailSenderName
            : company.DisplayName;

        var senderEmail = !string.IsNullOrWhiteSpace(company.EmailSenderEmail)
            ? company.EmailSenderEmail
            : company.Email;

        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(request.RecipientEmail));

        AddRecipients(message.Cc, request.CcEmails);
        AddRecipients(message.Bcc, request.BccEmails);

        message.Subject = $"{timesheet.Title} - {timesheet.Customer.Name} ({timesheet.From:dd.MM.yyyy} - {timesheet.To:dd.MM.yyyy})";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildHtmlBody(company, timesheet, request.CustomMessage),
            TextBody = BuildTextBody(company, timesheet, request.CustomMessage)
        };

        bodyBuilder.Attachments.Add(attachmentName, attachmentBytes, ContentType.Parse(contentType));
        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }

    private static string BuildHtmlBody(Company company, TimesheetDto timesheet, string? customMessage)
    {
        var sb = new StringBuilder();
        var greeting = string.IsNullOrWhiteSpace(company.EmailGreeting) ? "Hallo" : company.EmailGreeting;
        var closing = string.IsNullOrWhiteSpace(company.EmailClosing) ? "Viele Grüße" : company.EmailClosing;

        sb.Append($"<p>{greeting},</p>");
        sb.Append($"<p>anbei erhalten Sie den {timesheet.Title} für <strong>{timesheet.Customer.Name}</strong> im Zeitraum <strong>{timesheet.From:dd.MM.yyyy} - {timesheet.To:dd.MM.yyyy}</strong>.</p>");

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.Append($"<p>{customMessage}</p>");
        }

        if (!string.IsNullOrWhiteSpace(company.EmailSignature))
        {
            sb.Append($"<p>{closing},<br/>{company.EmailSignature}</p>");
        }
        else
        {
            sb.Append($"<p>{closing},<br/>{company.DisplayName}</p>");
        }

        return sb.ToString();
    }

    private static string BuildTextBody(Company company, TimesheetDto timesheet, string? customMessage)
    {
        var greeting = string.IsNullOrWhiteSpace(company.EmailGreeting) ? "Hallo" : company.EmailGreeting;
        var closing = string.IsNullOrWhiteSpace(company.EmailClosing) ? "Viele Grüße" : company.EmailClosing;

        var sb = new StringBuilder();
        sb.AppendLine(greeting);
        sb.AppendLine();
        sb.AppendLine($"Anbei erhalten Sie den {timesheet.Title} für {timesheet.Customer.Name} im Zeitraum {timesheet.From:dd.MM.yyyy} - {timesheet.To:dd.MM.yyyy}.");
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.AppendLine();
            sb.AppendLine(customMessage);
        }
        sb.AppendLine();
        sb.AppendLine(closing);
        sb.AppendLine(string.IsNullOrWhiteSpace(company.EmailSignature) ? company.DisplayName : company.EmailSignature);
        return sb.ToString();
    }

    private static void AddRecipients(InternetAddressList list, string? emails)
    {
        if (string.IsNullOrWhiteSpace(emails))
        {
            return;
        }

        var addresses = emails
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Trim())
            .Where(e => !string.IsNullOrWhiteSpace(e));

        foreach (var address in addresses)
        {
            list.Add(MailboxAddress.Parse(address));
        }
    }
}

public class TimesheetEmailRequest
{
    public int CustomerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int? ProjectId { get; set; }
    public decimal? HourlyRate { get; set; }
    public string? Title { get; set; }
    public string? FileName { get; set; }
    public List<int>? EntryIds { get; set; }
    public TimesheetAttachmentFormat Format { get; set; } = TimesheetAttachmentFormat.Pdf;
    public string RecipientEmail { get; set; } = string.Empty;
    public string? CustomMessage { get; set; }
    public string? CcEmails { get; set; }
    public string? BccEmails { get; set; }
}

