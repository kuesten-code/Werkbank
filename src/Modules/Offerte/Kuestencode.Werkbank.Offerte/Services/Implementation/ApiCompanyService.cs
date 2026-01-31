using Kuestencode.Core.Enums;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Shared.ApiClients;

namespace Kuestencode.Werkbank.Offerte.Services;

/// <summary>
/// Implementation of ICompanyService that communicates with Host via HTTP API.
/// Used when Offerte runs as a standalone microservice.
/// </summary>
public class ApiCompanyService : ICompanyService
{
    private readonly IHostApiClient _hostApiClient;
    private readonly ILogger<ApiCompanyService> _logger;

    public ApiCompanyService(IHostApiClient hostApiClient, ILogger<ApiCompanyService> logger)
    {
        _hostApiClient = hostApiClient;
        _logger = logger;
    }

    public async Task<Company> GetCompanyAsync()
    {
        try
        {
            var companyDto = await _hostApiClient.GetCompanyAsync();
            if (companyDto == null)
            {
                throw new InvalidOperationException("Company data not found in Host");
            }

            return MapToCompany(companyDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching company data from Host API");
            throw;
        }
    }

    public async Task<Company> UpdateCompanyAsync(Company company)
    {
        var request = new Kuestencode.Shared.Contracts.Host.UpdateCompanyRequest
        {
            OwnerFullName = company.OwnerFullName,
            BusinessName = company.BusinessName,
            Address = company.Address,
            PostalCode = company.PostalCode,
            City = company.City,
            Country = company.Country,
            TaxNumber = company.TaxNumber,
            VatId = company.VatId,
            IsKleinunternehmer = company.IsKleinunternehmer,
            BankName = company.BankName,
            BankAccount = company.BankAccount,
            Bic = company.Bic,
            AccountHolder = company.AccountHolder,
            Email = company.Email,
            Phone = company.Phone,
            Website = company.Website,
            DefaultPaymentTermDays = company.DefaultPaymentTermDays,
            InvoiceNumberPrefix = company.InvoiceNumberPrefix,
            FooterText = company.FooterText,
            EndpointId = company.EndpointId,
            EndpointSchemeId = company.EndpointSchemeId,
            SmtpHost = company.SmtpHost,
            SmtpPort = company.SmtpPort,
            SmtpUseSsl = company.SmtpUseSsl,
            SmtpUsername = company.SmtpUsername,
            SmtpPassword = company.SmtpPassword,
            EmailSenderEmail = company.EmailSenderEmail,
            EmailSenderName = company.EmailSenderName,
            EmailSignature = company.EmailSignature,
            EmailLayout = company.EmailLayout.ToString(),
            EmailPrimaryColor = company.EmailPrimaryColor,
            EmailAccentColor = company.EmailAccentColor,
            EmailGreeting = company.EmailGreeting,
            EmailClosing = company.EmailClosing,
            PdfLayout = company.PdfLayout.ToString(),
            PdfPrimaryColor = company.PdfPrimaryColor,
            PdfAccentColor = company.PdfAccentColor,
            PdfHeaderText = company.PdfHeaderText,
            PdfFooterText = company.PdfFooterText,
            PdfPaymentNotice = company.PdfPaymentNotice
        };

        await _hostApiClient.UpdateCompanyAsync(request);
        return await GetCompanyAsync();
    }

    public async Task<bool> HasCompanyDataAsync()
    {
        var company = await GetCompanyAsync();
        return !string.IsNullOrWhiteSpace(company.OwnerFullName);
    }

    public async Task<bool> IsEmailConfiguredAsync()
    {
        var company = await GetCompanyAsync();
        return !string.IsNullOrWhiteSpace(company.SmtpHost) &&
               company.SmtpPort.HasValue &&
               company.SmtpPort.Value > 0 &&
               !string.IsNullOrWhiteSpace(company.SmtpUsername);
    }

    private static Company MapToCompany(Kuestencode.Shared.Contracts.Host.CompanyDto dto)
    {
        var emailLayout = Enum.TryParse<EmailLayout>(dto.EmailLayout, out var parsedEmailLayout)
            ? parsedEmailLayout
            : EmailLayout.Klar;

        var pdfLayout = Enum.TryParse<PdfLayout>(dto.PdfLayout, out var parsedPdfLayout)
            ? parsedPdfLayout
            : PdfLayout.Klar;

        return new Company
        {
            Id = dto.Id,
            OwnerFullName = dto.OwnerFullName,
            BusinessName = dto.BusinessName,
            Address = dto.Address,
            PostalCode = dto.PostalCode,
            City = dto.City,
            Country = dto.Country,
            Phone = dto.Phone,
            Email = dto.Email,
            Website = dto.Website,
            TaxNumber = dto.TaxNumber,
            VatId = dto.VatId,
            IsKleinunternehmer = dto.IsKleinunternehmer,
            BankName = dto.BankName,
            BankAccount = dto.BankAccount,
            Bic = dto.Bic,
            AccountHolder = dto.AccountHolder,
            SmtpHost = dto.SmtpHost,
            SmtpPort = dto.SmtpPort,
            SmtpUseSsl = dto.SmtpUseSsl,
            SmtpUsername = dto.SmtpUsername,
            SmtpPassword = dto.SmtpPassword,
            EmailSenderEmail = dto.EmailSenderEmail,
            EmailSenderName = dto.EmailSenderName,
            EmailSignature = dto.EmailSignature,
            DefaultPaymentTermDays = dto.DefaultPaymentTermDays,
            InvoiceNumberPrefix = dto.InvoiceNumberPrefix,
            FooterText = dto.FooterText,
            LogoData = dto.LogoData,
            LogoContentType = dto.LogoContentType,
            EndpointId = dto.EndpointId,
            EndpointSchemeId = dto.EndpointSchemeId,
            EmailLayout = emailLayout,
            EmailPrimaryColor = dto.EmailPrimaryColor,
            EmailAccentColor = dto.EmailAccentColor,
            EmailGreeting = dto.EmailGreeting,
            EmailClosing = dto.EmailClosing,
            PdfLayout = pdfLayout,
            PdfPrimaryColor = dto.PdfPrimaryColor,
            PdfAccentColor = dto.PdfAccentColor,
            PdfHeaderText = dto.PdfHeaderText,
            PdfFooterText = dto.PdfFooterText,
            PdfPaymentNotice = dto.PdfPaymentNotice
        };
    }
}
