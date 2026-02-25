using System.Text;
using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Rapport.IntegrationTests.TestDoubles;
using Kuestencode.Rapport.Services;
using Kuestencode.Shared.Contracts.Rapport;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Rapport.IntegrationTests;

public class CsvExportTests : IClassFixture<RapportWebApplicationFactory>
{
    private readonly RapportWebApplicationFactory _factory;

    public CsvExportTests(RapportWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CsvExport_ShouldUseBerlinTimeForStartAndEnd()
    {
        using var scope = _factory.Services.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<TestCustomerService>();
        var timeEntryService = scope.ServiceProvider.GetRequiredService<TimeEntryService>();
        var csvService = scope.ServiceProvider.GetRequiredService<TimesheetCsvService>();

        var customer = await customerService.CreateAsync(new Customer { Name = "Delta GmbH", CustomerNumber = "C-4004" });
        var startUtc = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        var endUtc = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        await timeEntryService.CreateManualEntryAsync(startUtc, endUtc, null, customer.Id, "Zeitzonen-Test");

        var request = new TimesheetExportRequestDto
        {
            CustomerId = customer.Id,
            From = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        };

        var (bytes, _) = await csvService.GenerateAsync(request);
        var csv = Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("15.01.2026");
        csv.Should().Contain(";09:00;11:00;");
    }
}
