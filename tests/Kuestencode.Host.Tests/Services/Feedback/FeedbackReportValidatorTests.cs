using FluentAssertions;
using Kuestencode.Shared.Contracts.Feedback;
using Kuestencode.Werkbank.Host.Services.Feedback;
using Xunit;

namespace Kuestencode.Host.Tests.Services.Feedback;

public class FeedbackReportValidatorTests
{
    [Fact]
    public void Validate_VollstaendigerBug_IstGueltig()
    {
        FeedbackReportValidator.Validate(FeedbackTestData.Bug()).Should().BeEmpty();
    }

    [Fact]
    public void Validate_WunschOhneModulUndErwartung_IstGueltig()
    {
        FeedbackReportValidator.Validate(FeedbackTestData.Wish()).Should().BeEmpty();
    }

    [Fact]
    public void Validate_WunschMitModul_IstGueltig()
    {
        var request = FeedbackTestData.Wish();
        request.Module = "Rapport";

        FeedbackReportValidator.Validate(request).Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BugOhneModul_IstUngueltig(string? module)
    {
        var request = FeedbackTestData.Bug();
        request.Module = module;

        FeedbackReportValidator.Validate(request).Should().Contain("Modul ist bei einem Bug erforderlich.");
    }

    [Fact]
    public void Validate_BugOhneErwartung_IstUngueltig()
    {
        var request = FeedbackTestData.Bug();
        request.Expected = null;

        FeedbackReportValidator.Validate(request).Should().Contain("Erwartung ist bei einem Bug erforderlich.");
    }

    [Fact]
    public void Validate_BugOhneIstZustand_IstUngueltig()
    {
        var request = FeedbackTestData.Bug();
        request.Actual = "";

        FeedbackReportValidator.Validate(request).Should().Contain("Ist-Zustand ist erforderlich.");
    }

    [Fact]
    public void Validate_WunschOhneBeschreibung_IstUngueltig()
    {
        var request = FeedbackTestData.Wish();
        request.Actual = " ";

        FeedbackReportValidator.Validate(request).Should().Contain("Beschreibung ist erforderlich.");
    }

    [Theory]
    [InlineData(FeedbackType.Bug)]
    [InlineData(FeedbackType.Wunsch)]
    public void Validate_OhneTitel_IstUngueltig(FeedbackType type)
    {
        var request = type == FeedbackType.Bug ? FeedbackTestData.Bug() : FeedbackTestData.Wish();
        request.Title = "";

        FeedbackReportValidator.Validate(request).Should().Contain("Titel ist erforderlich.");
    }

    [Fact]
    public void Validate_ZuLangeFelder_SindUngueltig()
    {
        var request = FeedbackTestData.Bug();
        request.Title = new string('x', FeedbackReportValidator.TitleMaxLength + 1);
        request.Module = new string('x', FeedbackReportValidator.ModuleMaxLength + 1);
        request.Expected = new string('x', FeedbackReportValidator.TextMaxLength + 1);
        request.Actual = new string('x', FeedbackReportValidator.TextMaxLength + 1);

        FeedbackReportValidator.Validate(request).Should().HaveCount(4);
    }

    [Fact]
    public void Validate_OhneClientReportIdUndMitUnbekanntemTyp_IstUngueltig()
    {
        var request = FeedbackTestData.Wish();
        request.ClientReportId = Guid.Empty;
        request.Type = (FeedbackType)99;

        FeedbackReportValidator.Validate(request).Should().Contain(new[] { "ClientReportId fehlt.", "Unbekannter Meldungstyp." });
    }

    [Theory]
    [InlineData(null, 1)]
    [InlineData("  ", 1)]
    [InlineData("Danke!", 0)]
    public void ValidateComment_PrueftLeerenText(string? text, int expectedErrors)
    {
        FeedbackReportValidator.ValidateComment(text).Should().HaveCount(expectedErrors);
    }

    [Fact]
    public void ValidateComment_ZuLangerText_IstUngueltig()
    {
        FeedbackReportValidator.ValidateComment(new string('x', FeedbackReportValidator.TextMaxLength + 1)).Should().HaveCount(1);
    }
}
