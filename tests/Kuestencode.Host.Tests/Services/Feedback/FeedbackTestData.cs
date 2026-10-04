using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Data;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Kuestencode.Host.Tests.Services.Feedback;

internal static class FeedbackTestData
{
    public static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };
    public static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF, 0xE0, 0x00 };
    public static readonly byte[] Webp = "RIFF\0\0\0\0WEBPVP8 "u8.ToArray();
    public static readonly byte[] Pdf = "%PDF-1.7\n"u8.ToArray();
    public static readonly byte[] Exe = { 0x4D, 0x5A, 0x90, 0x00 };

    public static HostDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HostDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    public static FeedbackFileStore CreateFileStore(string basePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feedback:StoragePath"] = basePath })
            .Build();
        return new FeedbackFileStore(configuration, Mock.Of<IWebHostEnvironment>());
    }

    public static CreateFeedbackReportRequest Bug(Guid? clientReportId = null) => new()
    {
        ClientReportId = clientReportId ?? Guid.NewGuid(),
        Type = FeedbackType.Bug,
        Module = "Faktura",
        Title = "Rechnung lässt sich nicht speichern",
        Expected = "Rechnung wird gespeichert",
        Actual = "Fehlermeldung beim Speichern",
        ReporterName = "Erika Muster",
        PageUrl = "https://kunde.local/faktura/invoices/1",
        AppVersion = "2.16.0"
    };

    public static CreateFeedbackReportRequest Wish(Guid? clientReportId = null) => new()
    {
        ClientReportId = clientReportId ?? Guid.NewGuid(),
        Type = FeedbackType.Wunsch,
        Module = null,
        Title = "Dunkles PDF-Layout",
        Expected = null,
        Actual = "Ein zusätzliches PDF-Layout wäre schön",
        ReporterName = "Max Beispiel",
        PageUrl = "https://kunde.local/",
        AppVersion = "2.16.0"
    };
}
