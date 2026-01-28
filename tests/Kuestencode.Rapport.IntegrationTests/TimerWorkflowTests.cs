using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Rapport.Services;
using Kuestencode.Rapport.IntegrationTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Rapport.IntegrationTests;

public class TimerWorkflowTests : IClassFixture<RapportWebApplicationFactory>
{
    private readonly RapportWebApplicationFactory _factory;

    public TimerWorkflowTests(RapportWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartStopTimer_ShouldPersistEntry()
    {
        using var scope = _factory.Services.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<TestCustomerService>();
        var timerService = scope.ServiceProvider.GetRequiredService<TimerService>();
        var timeEntryService = scope.ServiceProvider.GetRequiredService<TimeEntryService>();

        var customer = await customerService.CreateAsync(new Customer { Name = "Acme GmbH", CustomerNumber = "C-1001" });

        var running = await timerService.StartTimerAsync(null, customer.Id, "Initial");
        running.CustomerId.Should().Be(customer.Id);
        running.EndTime.Should().BeNull();

        var stopped = await timerService.StopTimerAsync(running.Id, "Final");
        stopped.EndTime.Should().NotBeNull();
        stopped.Description.Should().Be("Final");

        var entries = await timeEntryService.GetEntriesAsync(null, null, new[] { customer.Id }, null, null, null);
        entries.Should().ContainSingle();
        entries[0].Status.Should().Be(Kuestencode.Rapport.Models.TimeEntryStatus.Stopped);
    }
}
