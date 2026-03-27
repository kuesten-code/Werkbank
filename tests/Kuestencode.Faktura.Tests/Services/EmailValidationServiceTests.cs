using FluentAssertions;
using Kuestencode.Faktura.Services;
using Xunit;

namespace Kuestencode.Faktura.Tests.Services;

public class EmailValidationServiceTests
{
    private readonly EmailValidationService _service = new();

    // ─── Null / leer ──────────────────────────────────────────────────────────

    [Fact]
    public void ValidateEmailList_NullEingabe_GueltigUndLeer()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList(null);
        isValid.Should().BeTrue();
        emails.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateEmailList_LeerString_GueltigUndLeer()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("   ");
        isValid.Should().BeTrue();
        emails.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    // ─── Einzelne E-Mail ──────────────────────────────────────────────────────

    [Fact]
    public void ValidateEmailList_EineGueltigeEmail_Gueltig()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("test@example.com");
        isValid.Should().BeTrue();
        emails.Should().ContainSingle().Which.Should().Be("test@example.com");
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateEmailList_EineUngueltigeEmail_Ungueltig()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("kein-email");
        isValid.Should().BeFalse();
        emails.Should().BeEmpty();
        errors.Should().ContainSingle();
    }

    // ─── Mehrere E-Mails ──────────────────────────────────────────────────────

    [Fact]
    public void ValidateEmailList_MehrereGueltigKommagetrennt_AlleGueltig()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("a@example.com,b@example.com");
        isValid.Should().BeTrue();
        emails.Should().HaveCount(2);
        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateEmailList_MehrereGueltigSemikolongetrennt_AlleGueltig()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("a@example.com;b@example.com");
        isValid.Should().BeTrue();
        emails.Should().HaveCount(2);
    }

    [Fact]
    public void ValidateEmailList_EineGueltigEineUngueltig_Ungueltig()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("gut@example.com,schlecht");
        isValid.Should().BeFalse();
        emails.Should().ContainSingle().Which.Should().Be("gut@example.com");
        errors.Should().ContainSingle();
    }

    [Fact]
    public void ValidateEmailList_MehrereUngueltige_AlleInFehler()
    {
        var (isValid, emails, errors) = _service.ValidateEmailList("x,y,z");
        isValid.Should().BeFalse();
        emails.Should().BeEmpty();
        errors.Should().HaveCount(3);
    }

    // ─── Whitespace-Handling ──────────────────────────────────────────────────

    [Fact]
    public void ValidateEmailList_EmailMitLeerzeichen_WirdGetrimmt()
    {
        var (isValid, emails, _) = _service.ValidateEmailList("  test@example.com  ");
        isValid.Should().BeTrue();
        emails.Should().ContainSingle().Which.Should().Be("test@example.com");
    }
}
