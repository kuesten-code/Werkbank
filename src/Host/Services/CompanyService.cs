using Kuestencode.Core.Enums;
using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;
using Kuestencode.Werkbank.Host.Data;
using Microsoft.EntityFrameworkCore;

namespace Kuestencode.Werkbank.Host.Services;

/// <summary>
/// Service für Firmenstammdaten-Verwaltung.
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly HostDbContext _context;
    private readonly PasswordEncryptionService _passwordEncryption;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(HostDbContext context, PasswordEncryptionService passwordEncryption, ILogger<CompanyService> logger)
    {
        _context = context;
        _passwordEncryption = passwordEncryption;
        _logger = logger;
    }

    public async Task<Company> GetCompanyAsync()
    {
        var company = await _context.Companies
            .Include(c => c.AdditionalBankAccounts)
            .FirstOrDefaultAsync();

        if (company == null)
        {
            // Create default company if none exists
            company = new Company
            {
                OwnerFullName = "Max Mustermann",
                BusinessName = "IT-Solutions Mustermann",
                Address = "",
                PostalCode = "",
                City = "",
                Country = "Deutschland",
                TaxNumber = "",
                Email = "",
                BankName = "",
                BankAccount = "",
                IsKleinunternehmer = true,
                DefaultPaymentTermDays = 14,
                EmailLayout = EmailLayout.Klar,
                EmailPrimaryColor = "#0F2A3D",
                EmailAccentColor = "#3FA796",
                EmailGreeting = "Sehr geehrte Damen und Herren,\n\nanbei erhalten Sie Ihre Rechnung.",
                EmailClosing = "Mit freundlichen Grüßen\n\n{{Firmenname}}",
                PdfLayout = PdfLayout.Klar,
                PdfPrimaryColor = "#1f3a5f",
                PdfAccentColor = "#3FA796",
                PdfPaymentNotice = "Bitte überweisen Sie den Betrag bis zum {{Faelligkeitsdatum}} auf unser Konto."
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
        }

        // Set AccountHolder to OwnerFullName if not set
        if (string.IsNullOrWhiteSpace(company.AccountHolder))
        {
            company.AccountHolder = company.OwnerFullName;
        }

        return company;
    }

    public async Task<Company> UpdateCompanyAsync(Company company)
    {
        var existing = await _context.Companies.FindAsync(company.Id);

        if (existing == null)
        {
            throw new InvalidOperationException("Firma nicht gefunden");
        }

        // Update all properties
        existing.OwnerFullName = company.OwnerFullName;
        existing.BusinessName = company.BusinessName;
        existing.Address = company.Address;
        existing.PostalCode = company.PostalCode;
        existing.City = company.City;
        existing.Country = company.Country;
        existing.TaxNumber = company.TaxNumber;
        existing.VatId = company.VatId;
        existing.IsKleinunternehmer = company.IsKleinunternehmer;
        existing.BankName = company.BankName;
        existing.BankAccount = company.BankAccount;
        existing.Bic = company.Bic;
        existing.AccountHolder = company.AccountHolder;
        existing.Email = company.Email;
        existing.Phone = company.Phone;
        existing.Website = company.Website;
        existing.DefaultPaymentTermDays = company.DefaultPaymentTermDays;
        existing.FooterText = company.FooterText;
        existing.LogoData = company.LogoData;
        existing.LogoContentType = company.LogoContentType;

        // Update SMTP email settings
        existing.SmtpHost = company.SmtpHost;
        existing.SmtpPort = company.SmtpPort;
        existing.SmtpUseSsl = company.SmtpUseSsl;
        existing.SmtpUsername = company.SmtpUsername;
        if (!string.IsNullOrWhiteSpace(company.SmtpPassword))
        {
            var plaintext = _passwordEncryption.Decrypt(company.SmtpPassword);
            existing.SmtpPassword = _passwordEncryption.Encrypt(plaintext);
        }
        existing.EmailSenderEmail = company.EmailSenderEmail;
        existing.EmailSenderName = company.EmailSenderName;
        existing.EmailSignature = company.EmailSignature;

        // Update email customization
        existing.EmailLayout = company.EmailLayout;
        existing.EmailPrimaryColor = company.EmailPrimaryColor;
        existing.EmailAccentColor = company.EmailAccentColor;
        existing.EmailGreeting = company.EmailGreeting;
        existing.EmailClosing = company.EmailClosing;

        // Update PDF customization
        existing.PdfLayout = company.PdfLayout;
        existing.PdfPrimaryColor = company.PdfPrimaryColor;
        existing.PdfAccentColor = company.PdfAccentColor;
        existing.PdfHeaderText = company.PdfHeaderText;
        existing.PdfFooterText = company.PdfFooterText;
        existing.PdfPaymentNotice = company.PdfPaymentNotice;

        // Snapshot additional bank accounts before EF can touch the collection.
        var newAccounts = company.AdditionalBankAccounts
            .Select(a => (a.BankName, a.Iban, a.Bic, a.AccountHolder, a.SortOrder))
            .ToList();

        // Detach all tracked AdditionalBankAccount entries so SaveChanges ignores them.
        foreach (var entry in _context.ChangeTracker.Entries<AdditionalBankAccount>().ToList())
            entry.State = EntityState.Detached;

        // Detach() leaves the (now stale) instances sitting in the in-memory collection.
        // Since the DbContext lives for the whole Blazor-Server circuit, a leftover instance
        // here would collide with the next newly-added (still Id=0) entry on the following
        // save and make EF Core throw "another instance with the same key value is already
        // being tracked". Clear the collection and mark it unloaded so the next GetCompanyAsync
        // repopulates it cleanly from the database.
        existing.AdditionalBankAccounts.Clear();
        _context.Entry(existing).Collection(c => c.AdditionalBankAccounts).IsLoaded = false;

        try
        {
            // Save Company entity only — additional bank accounts handled via raw SQL below
            // to avoid EF relationship fixup modifying the navigation collection mid-render.
            await _context.SaveChangesAsync();

            await _context.Database.ExecuteSqlAsync(
                $"DELETE FROM host.\"AdditionalBankAccounts\" WHERE \"CompanyId\" = {existing.Id}");

            for (int i = 0; i < newAccounts.Count; i++)
            {
                var (bankName, iban, bic, accountHolder, sortOrder) = newAccounts[i];
                await _context.Database.ExecuteSqlAsync(
                    $"INSERT INTO host.\"AdditionalBankAccounts\" (\"CompanyId\", \"BankName\", \"Iban\", \"Bic\", \"AccountHolder\", \"SortOrder\") VALUES ({existing.Id}, {bankName}, {iban}, {bic}, {accountHolder}, {sortOrder})");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Speichern der Firma/Bankverbindungen (CompanyId={CompanyId}, Bankverbindungen={Count})",
                existing.Id, newAccounts.Count);
            throw;
        }

        return existing;
    }

    public async Task<bool> HasCompanyDataAsync()
    {
        var company = await _context.Companies.FirstOrDefaultAsync();

        if (company == null)
        {
            return false;
        }

        return company.HasRequiredData();
    }

    public async Task<bool> IsEmailConfiguredAsync()
    {
        var company = await _context.Companies.FirstOrDefaultAsync();

        if (company == null)
        {
            return false;
        }

        return company.IsEmailConfigured();
    }
}
