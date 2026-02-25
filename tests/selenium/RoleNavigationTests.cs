using Xunit;

namespace Kuestencode.SeleniumTests;

public class RoleNavigationTests : SeleniumTestBase
{
    [Fact]
    public void Admin_SeesSettingsAndTeam()
    {
        Require(!string.IsNullOrWhiteSpace(Config.AdminEmail) && !string.IsNullOrWhiteSpace(Config.AdminPassword),
            "Missing env vars: SELENIUM_ADMIN_EMAIL / SELENIUM_ADMIN_PASSWORD");

        Login(Config.AdminEmail!, Config.AdminPassword!);
        Navigate("/");

        var labels = ReadNavLabels();

        Assert.Contains("Rapport", labels);
        Assert.Contains("Mitarbeiter", labels);
        Assert.Contains("Einstellungen", labels);
    }

    [Fact]
    public void Buero_DoesNotSeeSettingsOrTeam()
    {
        Require(!string.IsNullOrWhiteSpace(Config.BueroEmail) && !string.IsNullOrWhiteSpace(Config.BueroPassword),
            "Missing env vars: SELENIUM_BUERO_EMAIL / SELENIUM_BUERO_PASSWORD");

        Login(Config.BueroEmail!, Config.BueroPassword!);
        Navigate("/");

        var labels = ReadNavLabels();

        Assert.Contains("Rapport", labels);
        Assert.DoesNotContain("Mitarbeiter", labels);
        Assert.DoesNotContain("Einstellungen", labels);
    }

    [Fact]
    public void Mitarbeiter_SeesOnlyRapportArea()
    {
        Require(!string.IsNullOrWhiteSpace(Config.MitarbeiterEmail) && !string.IsNullOrWhiteSpace(Config.MitarbeiterPassword),
            "Missing env vars: SELENIUM_MITARBEITER_EMAIL / SELENIUM_MITARBEITER_PASSWORD");

        Login(Config.MitarbeiterEmail!, Config.MitarbeiterPassword!);
        Navigate("/");

        var labels = ReadNavLabels();

        Assert.Contains("Rapport", labels);
        Assert.DoesNotContain("Mitarbeiter", labels);
        Assert.DoesNotContain("Einstellungen", labels);

        var blocked = new[] { "Faktura", "Offerte", "Acta", "Recepta", "Rechnungen", "Angebote", "Projekte", "Belege" };
        foreach (var blockedLabel in blocked)
        {
            Assert.DoesNotContain(blockedLabel, labels);
        }
    }
}
