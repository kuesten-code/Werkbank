using FluentAssertions;
using Kuestencode.Core.Models;
using Kuestencode.Rapport.Services;
using Kuestencode.Rapport.IntegrationTests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Rapport.IntegrationTests;

public class ProjectIntegrationTests : IClassFixture<RapportWebApplicationFactory>
{
    private readonly RapportWebApplicationFactory _factory;

    public ProjectIntegrationTests(RapportWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartTimer_WithProject_ShouldUseProjectCustomer()
    {
        using var scope = _factory.Services.CreateScope();
        var customerService = scope.ServiceProvider.GetRequiredService<TestCustomerService>();
        var projectService = scope.ServiceProvider.GetRequiredService<TestProjectService>();
        var timerService = scope.ServiceProvider.GetRequiredService<TimerService>();

        var customer = await customerService.CreateAsync(new Customer { Name = "Delta OHG", CustomerNumber = "C-4004" });
        projectService.AddProject(new TestProjectService.TestProject
        {
            Id = 20,
            Name = "Projekt B",
            CustomerId = customer.Id,
            CustomerName = customer.Name
        });

        var entry = await timerService.StartTimerAsync(20, null, "Projektstart");
        entry.CustomerId.Should().Be(customer.Id);
        entry.ProjectId.Should().Be(20);
    }
}
