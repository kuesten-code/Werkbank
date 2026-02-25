using Xunit;

namespace Kuestencode.SeleniumTests;

public class FakturaPagesSmokeTests : SeleniumTestBase
{
    public static IEnumerable<object[]> AllFakturaRoutes()
    {
        yield return ["/faktura"];
        yield return ["/faktura/invoices"];
        yield return ["/faktura/invoices/create"];
        yield return ["/faktura/invoices/edit/1"];
        yield return ["/faktura/invoices/details/1"];
        yield return ["/faktura/settings/email-anpassung"];
        yield return ["/faktura/settings/pdf-anpassung"];
    }

    [Theory]
    [MemberData(nameof(AllFakturaRoutes))]
    public void FakturaRoute_Unauthenticated_RedirectsToLogin_AndDoesNotCrash(string route)
    {
        NavigateAndAssertNoUnhandledError(route);
        Assert.Contains("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(AllFakturaRoutes))]
    public void FakturaRoute_AsAdmin_DoesNotCrash(string route)
    {
        Require(!string.IsNullOrWhiteSpace(Config.AdminEmail) && !string.IsNullOrWhiteSpace(Config.AdminPassword),
            "Missing env vars: SELENIUM_ADMIN_EMAIL / SELENIUM_ADMIN_PASSWORD");

        Login(Config.AdminEmail!, Config.AdminPassword!);
        NavigateAndAssertNoUnhandledError(route);
        Assert.DoesNotContain("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
    }
}
