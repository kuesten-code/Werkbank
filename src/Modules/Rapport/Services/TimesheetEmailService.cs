using System.ComponentModel.DataAnnotations;
using System.Text;
using Kuestencode.Core.Interfaces;
using Kuestencode.Rapport.Models.Timesheets;
using Kuestencode.Shared.Contracts.Rapport;

namespace Kuestencode.Rapport.Services;

/// <summary>
/// Sends timesheet exports via email.
/// </summary>
public class TimesheetEmailService
{
    private readonly IEmailEngine _emailEngine;
    private readonly TimesheetPdfService _pdfService;
    private readonly TimesheetCsvService _csvService;
    private readonly TimesheetExportService _exportService;
    private readonly ILogger<TimesheetEmailService> _logger;

    public TimesheetEmailService(
        IEmailEngine emailEngine,
        TimesheetPdfService pdfService,
        TimesheetCsvService csvService,
        TimesheetExportService exportService,
        ILogger<TimesheetEmailService> logger)
    {
        _emailEngine = emailEngine;
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

        var subject = $"{timesheet.Title} - {timesheet.Customer.Name} ({timesheet.From:dd.MM.yyyy} - {timesheet.To:dd.MM.yyyy})";
        var attachment = new EmailAttachment
        {
            FileName = attachmentName,
            Content = attachmentBytes,
            ContentType = contentType
        };

        var success = await _emailEngine.SendEmailAsync(
            request.RecipientEmail,
            subject,
            BuildHtmlContent(timesheet, request.CustomMessage),
            BuildTextContent(timesheet, request.CustomMessage),
            new[] { attachment },
            request.CcEmails,
            request.BccEmails);

        if (!success)
        {
            throw new InvalidOperationException("E-Mail-Versand fehlgeschlagen.");
        }

        _logger.LogInformation("Timesheet email sent to {Recipient}", request.RecipientEmail);
    }

    private static string BuildHtmlContent(TimesheetDto timesheet, string? customMessage)
    {
        var sb = new StringBuilder();
        sb.Append($"<p>anbei erhalten Sie den {timesheet.Title} für <strong>{timesheet.Customer.Name}</strong> im Zeitraum <strong>{timesheet.From:dd.MM.yyyy} - {timesheet.To:dd.MM.yyyy}</strong>.</p>");

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.Append($"<p>{customMessage}</p>");
        }

        return sb.ToString();
    }

    private static string BuildTextContent(TimesheetDto timesheet, string? customMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Anbei erhalten Sie den {timesheet.Title} für {timesheet.Customer.Name} im Zeitraum {timesheet.From:dd.MM.yyyy} - {timesheet.To:dd.MM.yyyy}.");
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            sb.AppendLine();
            sb.AppendLine(customMessage);
        }
        return sb.ToString();
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

