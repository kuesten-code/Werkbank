using Microsoft.AspNetCore.Mvc;
using Kuestencode.Core.Enums;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Werkbank.Host.Services;

namespace Kuestencode.Werkbank.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _companyService;
    private readonly PasswordEncryptionService _passwordEncryption;

    public CompanyController(ICompanyService companyService, PasswordEncryptionService passwordEncryption)
    {
        _companyService = companyService;
        _passwordEncryption = passwordEncryption;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyDto>> GetCompany()
    {
        var company = await _companyService.GetCompanyAsync();
        if (company == null) return NotFound();
        return Ok(MapToDto(company));
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCompany([FromBody] UpdateCompanyRequest request)
    {
        var company = await _companyService.GetCompanyAsync();

        // Map request to entity
        company.OwnerFullName = request.OwnerFullName;
        company.BusinessName = request.BusinessName;
        company.Address = request.Address;
        company.PostalCode = request.PostalCode;
        company.City = request.City;
        company.Country = request.Country;
        company.TaxNumber = request.TaxNumber;
        company.VatId = request.VatId;
        company.IsKleinunternehmer = request.IsKleinunternehmer;
        company.BankName = request.BankName;
        company.BankAccount = request.BankAccount;
        company.Bic = request.Bic;
        company.AccountHolder = request.AccountHolder;
        company.Email = request.Email;
        company.Phone = request.Phone;
        company.Website = request.Website;
        company.DefaultPaymentTermDays = request.DefaultPaymentTermDays;
        company.InvoiceNumberPrefix = request.InvoiceNumberPrefix;
        company.FooterText = request.FooterText;
        company.EndpointId = request.EndpointId;
        company.EndpointSchemeId = request.EndpointSchemeId;

        if (request.SmtpHost != null) company.SmtpHost = request.SmtpHost;
        if (request.SmtpPort.HasValue) company.SmtpPort = request.SmtpPort;
        if (request.SmtpUseSsl.HasValue) company.SmtpUseSsl = request.SmtpUseSsl.Value;
        if (request.SmtpUsername != null) company.SmtpUsername = request.SmtpUsername;
        if (request.SmtpPassword != null) company.SmtpPassword = request.SmtpPassword;
        if (request.EmailSenderEmail != null) company.EmailSenderEmail = request.EmailSenderEmail;
        if (request.EmailSenderName != null) company.EmailSenderName = request.EmailSenderName;
        if (request.EmailSignature != null) company.EmailSignature = request.EmailSignature;

        if (!string.IsNullOrWhiteSpace(request.EmailLayout) &&
            Enum.TryParse<EmailLayout>(request.EmailLayout, out var emailLayout))
        {
            company.EmailLayout = emailLayout;
        }

        if (request.EmailPrimaryColor != null) company.EmailPrimaryColor = request.EmailPrimaryColor;
        if (request.EmailAccentColor != null) company.EmailAccentColor = request.EmailAccentColor;
        if (request.EmailGreeting != null) company.EmailGreeting = request.EmailGreeting;
        if (request.EmailClosing != null) company.EmailClosing = request.EmailClosing;

        if (!string.IsNullOrWhiteSpace(request.PdfLayout) &&
            Enum.TryParse<PdfLayout>(request.PdfLayout, out var pdfLayout))
        {
            company.PdfLayout = pdfLayout;
        }

        if (request.PdfPrimaryColor != null) company.PdfPrimaryColor = request.PdfPrimaryColor;
        if (request.PdfAccentColor != null) company.PdfAccentColor = request.PdfAccentColor;
        if (request.PdfHeaderText != null) company.PdfHeaderText = request.PdfHeaderText;
        if (request.PdfFooterText != null) company.PdfFooterText = request.PdfFooterText;
        if (request.PdfPaymentNotice != null) company.PdfPaymentNotice = request.PdfPaymentNotice;

        await _companyService.UpdateCompanyAsync(company);
        return NoContent();
    }

    private CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            OwnerFullName = company.OwnerFullName,
            BusinessName = company.BusinessName,
            DisplayName = company.DisplayName,
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
            LogoData = company.LogoData,
            LogoContentType = company.LogoContentType,
            EndpointId = company.EndpointId,
            EndpointSchemeId = company.EndpointSchemeId,
            SmtpHost = company.SmtpHost,
            SmtpPort = company.SmtpPort,
            SmtpUseSsl = company.SmtpUseSsl,
            SmtpUsername = company.SmtpUsername,
            SmtpPassword = _passwordEncryption.Decrypt(company.SmtpPassword ?? string.Empty),
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
    }
}
