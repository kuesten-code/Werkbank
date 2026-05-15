using Kuestencode.Werkbank.Acta.Data;
using Kuestencode.Werkbank.Acta.Data.Repositories;
using Kuestencode.Werkbank.Acta.Domain.Services;
using Kuestencode.Werkbank.Acta.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuestencode.Werkbank.Acta;

/// <summary>
/// Module definition for the Acta (Projektverwaltung) module.
/// Provides extension methods for service registration.
/// </summary>
public static class ActaModule
{
    /// <summary>
    /// Adds all Acta module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddActaModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with PostgreSQL (Acta-Schema)
        // Factory pattern avoids concurrency issues in Blazor Server (OnInitializedAsync + OnAfterRenderAsync overlap)
        services.AddDbContextFactory<ActaDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<ActaDbContext>>().CreateDbContext());

        // Register Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
        services.AddScoped<IProjektStundensatzRepository, ProjektStundensatzRepository>();

        // Register Domain Services
        services.AddScoped<ProjectStatusService>();

        // Register Application Services
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectTaskService, ProjectTaskService>();
        services.AddScoped<IStundensatzService, StundensatzService>();
        services.AddSingleton<ITeamMemberDirectory, HostTeamMemberDirectory>();

        return services;
    }

    /// <summary>
    /// Applies pending database migrations.
    /// </summary>
    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ActaDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await context.Database.MigrateAsync();
    }
}
