using System;
using System.Collections.Generic;
using System.Linq;

using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Rapport.Models;
using Kuestencode.Rapport.Models.Timesheets;

namespace Kuestencode.Rapport.Services;

public class TimesheetPreviewService
{
    private readonly ICustomerService _customerService;
    private readonly TimesheetPdfService _pdfService;

    public TimesheetPreviewService(
        ICustomerService customerService,
        TimesheetPdfService pdfService)
    {
        _customerService = customerService;
        _pdfService = pdfService;
    }

    public async Task<byte[]> GeneratePreviewAsync(RapportSettings settings)
    {
        var customer = (await _customerService.GetAllAsync()).FirstOrDefault();

        var customerInfo = new TimesheetCustomerInfoDto
        {
            Name = customer?.Name ?? "Musterkunde GmbH",
            CustomerNumber = customer?.CustomerNumber ?? "K-1001",
            Address = customer?.Address ?? "Musterstraße 12",
            PostalCode = customer?.PostalCode ?? "12345",
            City = customer?.City ?? "Musterstadt",
            Country = customer?.Country ?? "Deutschland"
        };

        var today = DateTime.Today;
        var entriesA = new List<TimesheetEntryDto>
        {
            new()
            {
                Date = today.AddDays(-3),
                StartTime = today.AddDays(-3).AddHours(9),
                EndTime = today.AddDays(-3).AddHours(12),
                Description = "Konzept & Planung",
                Duration = TimeSpan.FromHours(3)
            },
            new()
            {
                Date = today.AddDays(-2),
                StartTime = today.AddDays(-2).AddHours(10),
                EndTime = today.AddDays(-2).AddHours(14),
                Description = "Implementierung",
                Duration = TimeSpan.FromHours(4)
            }
        };

        var entriesB = new List<TimesheetEntryDto>
        {
            new()
            {
                Date = today.AddDays(-1),
                StartTime = today.AddDays(-1).AddHours(9),
                EndTime = today.AddDays(-1).AddHours(11),
                Description = "Abstimmung",
                Duration = TimeSpan.FromHours(2)
            }
        };

        var groupA = new TimesheetProjectGroupDto
        {
            ProjectId = 1,
            ProjectName = "Website-Relaunch",
            Entries = entriesA,
            SubtotalHours = entriesA.Sum(e => (decimal)e.Duration.TotalHours)
        };

        var groupB = new TimesheetProjectGroupDto
        {
            ProjectId = null,
            ProjectName = "Ohne Projekt",
            Entries = entriesB,
            SubtotalHours = entriesB.Sum(e => (decimal)e.Duration.TotalHours)
        };

        var totalHours = groupA.SubtotalHours + groupB.SubtotalHours;
        var hourlyRate = settings.DefaultHourlyRate > 0 ? settings.DefaultHourlyRate : 95m;

        if (!settings.ShowHourlyRateInPdf)
        {
            hourlyRate = 0m;
        }

        var dto = new TimesheetDto
        {
            Title = "Tätigkeitsnachweis",
            From = today.AddDays(-7),
            To = today,
            Customer = customerInfo,
            Groups = new List<TimesheetProjectGroupDto> { groupA, groupB },
            TotalHours = totalHours,
            HourlyRate = settings.ShowHourlyRateInPdf ? hourlyRate : null,
            TotalAmount = settings.CalculateTotalAmount && settings.ShowHourlyRateInPdf
                ? totalHours * hourlyRate
                : null
        };

        return await _pdfService.RenderAsync(dto);
    }

    /// <summary>
    /// Synchronous preview generation for use in preview callbacks where async is not supported.
    /// Requires pre-loaded company and settings.
    /// </summary>
    public byte[] GeneratePreview(RapportSettings settings, Company company)
    {
        var customerInfo = new TimesheetCustomerInfoDto
        {
            Name = "Musterkunde GmbH",
            CustomerNumber = "K-1001",
            Address = "Musterstraße 12",
            PostalCode = "12345",
            City = "Musterstadt",
            Country = "Deutschland"
        };

        var today = DateTime.Today;
        var entriesA = new List<TimesheetEntryDto>
        {
            new()
            {
                Date = today.AddDays(-3),
                StartTime = today.AddDays(-3).AddHours(9),
                EndTime = today.AddDays(-3).AddHours(12),
                Description = "Konzept & Planung",
                Duration = TimeSpan.FromHours(3)
            },
            new()
            {
                Date = today.AddDays(-2),
                StartTime = today.AddDays(-2).AddHours(10),
                EndTime = today.AddDays(-2).AddHours(14),
                Description = "Implementierung",
                Duration = TimeSpan.FromHours(4)
            }
        };

        var entriesB = new List<TimesheetEntryDto>
        {
            new()
            {
                Date = today.AddDays(-1),
                StartTime = today.AddDays(-1).AddHours(9),
                EndTime = today.AddDays(-1).AddHours(11),
                Description = "Abstimmung",
                Duration = TimeSpan.FromHours(2)
            }
        };

        var groupA = new TimesheetProjectGroupDto
        {
            ProjectId = 1,
            ProjectName = "Website-Relaunch",
            Entries = entriesA,
            SubtotalHours = entriesA.Sum(e => (decimal)e.Duration.TotalHours)
        };

        var groupB = new TimesheetProjectGroupDto
        {
            ProjectId = null,
            ProjectName = "Ohne Projekt",
            Entries = entriesB,
            SubtotalHours = entriesB.Sum(e => (decimal)e.Duration.TotalHours)
        };

        var totalHours = groupA.SubtotalHours + groupB.SubtotalHours;
        var hourlyRate = settings.DefaultHourlyRate > 0 ? settings.DefaultHourlyRate : 95m;

        if (!settings.ShowHourlyRateInPdf)
        {
            hourlyRate = 0m;
        }

        var dto = new TimesheetDto
        {
            Title = "Tätigkeitsnachweis",
            From = today.AddDays(-7),
            To = today,
            Customer = customerInfo,
            Groups = new List<TimesheetProjectGroupDto> { groupA, groupB },
            TotalHours = totalHours,
            HourlyRate = settings.ShowHourlyRateInPdf ? hourlyRate : null,
            TotalAmount = settings.CalculateTotalAmount && settings.ShowHourlyRateInPdf
                ? totalHours * hourlyRate
                : null
        };

        return _pdfService.Render(dto, company, settings);
    }
}
