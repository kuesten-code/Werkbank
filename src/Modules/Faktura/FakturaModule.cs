using Kuestencode.Faktura.Data;
using Kuestencode.Faktura.Data.Repositories;
using Kuestencode.Faktura.Services;
using Kuestencode.Faktura.Services.Email;
using Kuestencode.Faktura.Services.Pdf;
using Kuestencode.Faktura.Services.Pdf.Components;
using Kuestencode.Faktura.Services.Pdf.Layouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FakturaRepo = Kuestencode.Faktura.Data.Repositories;

namespace Kuestencode.Faktura;

/// <summary>
/// Module definition for the Faktura (Invoice) module.
/// Provides extension methods for service registration.
/// </summary>
public static class FakturaModule
{
    /// <summary>
    /// Adds all Faktura module services to the service collection.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddFakturaModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext with PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Repositories
        services.AddScoped(typeof(FakturaRepo.IRepository<>), typeof(Repository<>));
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        // Register Core Services (Company, Customer)
        services.AddScoped<Services.ICompanyService, CompanyService>();
        services.AddScoped<Services.ICustomerService, CustomerService>();

        // Register Faktura Services
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IXRechnungService, XRechnungService>();
        services.AddScoped<IPreviewService, PreviewService>();
        services.AddScoped<IEmailValidationService, EmailValidationService>();

        // Register Email Services
        services.AddScoped<IEmailTemplateRenderer, HtmlEmailTemplateRenderer>();
        services.AddScoped<IEmailAttachmentBuilder, EmailAttachmentBuilder>();
        services.AddScoped<ISmtpClient, SmtpClientWrapper>();
        services.AddScoped<IEmailMessageBuilder, EmailMessageBuilder>();
        services.AddScoped<Services.IEmailService, EmailService>();

        // Register PDF Services
        services.AddScoped<PdfTemplateEngine>();
        services.AddScoped<PdfQRCodeGenerator>();
        services.AddScoped<PdfSummaryBlockBuilder>();
        services.AddScoped<PdfPaymentInfoBuilder>();
        services.AddScoped<KlarLayoutRenderer>();
        services.AddScoped<StrukturiertLayoutRenderer>();
        services.AddScoped<BetontLayoutRenderer>();
        services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();

        // Register Singleton Services
        services.AddSingleton<PasswordEncryptionService>();

        // Register Background Services
        services.AddHostedService<InvoiceOverdueService>();

        return services;
    }

    /// <summary>
    /// Applies pending database migrations.
    /// </summary>
    public static async Task ApplyMigrationsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
}
