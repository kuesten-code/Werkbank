using FluentAssertions;
using Kuestencode.Werkbank.Offerte.Data;
using Kuestencode.Werkbank.Offerte.Domain.Entities;
using Kuestencode.Werkbank.Offerte.Domain.Enums;
using Kuestencode.Werkbank.Offerte.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kuestencode.Werkbank.Offerte.Tests.Services;

public class OfferteSettingsServiceTests : IDisposable
{
    private readonly OfferteDbContext _dbContext;

    public OfferteSettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<OfferteDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new OfferteDbContext(options);
    }

    public void Dispose() => _dbContext.Dispose();

    private OfferteSettingsService CreateService() => new(_dbContext);

    // ─── GetSettingsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetSettings_KeinEintrag_ErstelltDefaultEinstellungen()
    {
        var settings = await CreateService().GetSettingsAsync();

        settings.Should().NotBeNull();
        // Default values as defined in entity
        settings.EmailPrimaryColor.Should().Be("#0F2A3D");
        settings.PdfPrimaryColor.Should().Be("#1f3a5f");
    }

    [Fact]
    public async Task GetSettings_EintragVorhanden_GibtIhnZurueck()
    {
        var existing = new OfferteSettings
        {
            EmailPrimaryColor = "#112233",
            PdfPrimaryColor = "#445566"
        };
        _dbContext.Settings.Add(existing);
        await _dbContext.SaveChangesAsync();

        var settings = await CreateService().GetSettingsAsync();

        settings.EmailPrimaryColor.Should().Be("#112233");
        settings.PdfPrimaryColor.Should().Be("#445566");
    }

    [Fact]
    public async Task GetSettings_WirdNurEinmalErstellt()
    {
        await CreateService().GetSettingsAsync();
        await CreateService().GetSettingsAsync();

        var count = await _dbContext.Settings.CountAsync();
        count.Should().Be(1);
    }

    // ─── UpdateSettingsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSettings_KeinEintrag_ErstelltNeuen()
    {
        var settings = new OfferteSettings
        {
            EmailPrimaryColor = "#AABBCC",
            PdfLayout = PdfLayout.Strukturiert
        };

        await CreateService().UpdateSettingsAsync(settings);

        var count = await _dbContext.Settings.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateSettings_EintragVorhanden_AktualisiertAlle()
    {
        var existing = new OfferteSettings { EmailPrimaryColor = "#000000" };
        _dbContext.Settings.Add(existing);
        await _dbContext.SaveChangesAsync();

        var updated = new OfferteSettings
        {
            EmailPrimaryColor = "#FFFFFF",
            EmailAccentColor = "#FF0000",
            EmailGreeting = "Hallo",
            EmailClosing = "Tschüss",
            EmailLayout = EmailLayout.Strukturiert,
            PdfPrimaryColor = "#123456",
            PdfAccentColor = "#654321",
            PdfHeaderText = "Header",
            PdfFooterText = "Footer",
            PdfValidityNotice = "Gültig bis...",
            PdfLayout = PdfLayout.Betont
        };

        await CreateService().UpdateSettingsAsync(updated);

        var result = await _dbContext.Settings.FirstAsync();
        result.EmailPrimaryColor.Should().Be("#FFFFFF");
        result.EmailAccentColor.Should().Be("#FF0000");
        result.EmailGreeting.Should().Be("Hallo");
        result.PdfPrimaryColor.Should().Be("#123456");
        result.PdfLayout.Should().Be(PdfLayout.Betont);
        result.PdfFooterText.Should().Be("Footer");
    }

    [Fact]
    public async Task UpdateSettings_ErstelltKeinenZweitenEintrag()
    {
        var existing = new OfferteSettings();
        _dbContext.Settings.Add(existing);
        await _dbContext.SaveChangesAsync();

        await CreateService().UpdateSettingsAsync(new OfferteSettings { EmailPrimaryColor = "#111111" });
        await CreateService().UpdateSettingsAsync(new OfferteSettings { EmailPrimaryColor = "#222222" });

        var count = await _dbContext.Settings.CountAsync();
        count.Should().Be(1);
    }
}
