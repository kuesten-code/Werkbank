using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Rapport.Services;
using Kuestencode.Rapport.IntegrationTests.TestDoubles;
using Kuestencode.Shared.Contracts.Rapport;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Rapport.IntegrationTests;

public class PdfExportTests : IClassFixture<RapportWebApplicationFactory>
{
    private readonly RapportWebApplicationFactory _factory;

    public PdfExportTests(RapportWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PdfExport_WithProjectFilter_ShouldGeneratePdf()
    {
        using var scope = _factory.Services.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<TestCustomerService>();
        var projectService = scope.ServiceProvider.GetRequiredService<TestProjectService>();
        var timeEntryService = scope.ServiceProvider.GetRequiredService<TimeEntryService>();
        var pdfService = scope.ServiceProvider.GetRequiredService<TimesheetPdfService>();

        var customer = await customerService.CreateAsync(new Customer { Name = "Gamma GmbH", CustomerNumber = "C-3003" });
        projectService.AddProject(new TestProjectService.TestProject
        {
            Id = 10,
            Name = "Projekt A",
            CustomerId = customer.Id,
            CustomerName = customer.Name
        });

        var start = DateTime.UtcNow.AddHours(-5);
        var end = DateTime.UtcNow.AddHours(-3);
        await timeEntryService.CreateManualEntryAsync(start, end, 10, null, "Projektarbeit");
        await timeEntryService.CreateManualEntryAsync(end.AddMinutes(10), end.AddMinutes(40), null, customer.Id, "Ohne Projekt");

        var request = new TimesheetExportRequestDto
        {
            CustomerId = customer.Id,
            From = DateTime.UtcNow.AddDays(-1),
            To = DateTime.UtcNow,
            ProjectId = 10
        };

        var (bytes, _) = await pdfService.GenerateAsync(request);
        bytes.Length.Should().BeGreaterThan(1000);
    }
}
