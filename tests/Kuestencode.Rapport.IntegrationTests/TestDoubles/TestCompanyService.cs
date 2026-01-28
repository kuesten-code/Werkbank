using Kuestencode.Core.Interfaces;
using Kuestencode.Core.Models;

namespace Kuestencode.Rapport.IntegrationTests.TestDoubles;

public sealed class TestCompanyService : ICompanyService
{
    private Company _company = new()
    {
        Id = 1,
        OwnerFullName = "Kuestencode Werkbank",
        Address = "Musterstra?e 1",
        PostalCode = "12345",
        City = "Musterstadt",
        Country = "Deutschland",
        Email = "info@kuestencode.de",
        BankName = "Musterbank",
        BankAccount = "DE00000000000000000000",
        TaxNumber = "123/456/789",
        PdfPrimaryColor = "#1f3a5f",
        PdfAccentColor = "#3FA796",
        PdfLayout = Kuestencode.Core.Enums.PdfLayout.Klar
    };

    public Task<Company> GetCompanyAsync()
    {
        return Task.FromResult(_company);
    }

    public Task<Company> UpdateCompanyAsync(Company company)
    {
        _company = company;
        return Task.FromResult(company);
    }

    public Task<bool> HasCompanyDataAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> IsEmailConfiguredAsync()
    {
        return Task.FromResult(true);
    }
}
