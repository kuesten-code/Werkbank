using Kuestencode.Core.Interfaces;
using System.Collections.Generic;

using Kuestencode.Core.Models;
using Kuestencode.Rapport.Api;
using Kuestencode.Rapport.Data;
using Kuestencode.Rapport.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Kuestencode.Rapport.IntegrationTests;

public sealed class RapportWebApplicationFactory : WebApplicationFactory<ProgramApi>
{
    private readonly string _dbName = $"rapport-tests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");


        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["APPLY_MIGRATIONS"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<RapportDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<RapportDbContext>>();
            services.RemoveAll<DbContextOptions>();

            var npgsqlDescriptors = services
                .Where(d => d.ImplementationType?.Assembly.GetName().Name?.Contains("Npgsql") == true
                         || d.ServiceType.Assembly.GetName().Name?.Contains("Npgsql") == true)
                .ToList();
            foreach (var descriptor in npgsqlDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RapportDbContext>(options => options.UseInMemoryDatabase(_dbName));

            services.RemoveAll<IHostedService>();

            services.RemoveAll<ICustomerService>();
            services.RemoveAll<IProjectService>();
            services.RemoveAll<ICompanyService>();

            services.AddSingleton<TestDoubles.TestCustomerService>();
            services.AddSingleton<ICustomerService>(sp => sp.GetRequiredService<TestDoubles.TestCustomerService>());

            services.AddSingleton<TestDoubles.TestProjectService>();
            services.AddSingleton<IProjectService>(sp => sp.GetRequiredService<TestDoubles.TestProjectService>());

            services.AddSingleton<ICompanyService, TestDoubles.TestCompanyService>();
        });
    }
}
