using Xunit;

namespace Kuestencode.SeleniumTests;

public class HostPagesSmokeTests : SeleniumTestBase
{
    public static IEnumerable<object[]> AllHostRoutes()
    {
        // Public routes
        yield return ["/login", false];
        yield return ["/forgot-password", false];
        yield return ["/invite/test-token", false];
        yield return ["/reset/test-token", false];
        yield return ["/setup", false];
        yield return ["/m/test-token", false];
        yield return ["/m/test-token/rapport", false];

        // Protected routes
        yield return ["/", true];
        yield return ["/customers", true];
        yield return ["/customers/create", true];
        yield return ["/customers/edit/1", true];
        yield return ["/settings/auth", true];
        yield return ["/settings/company", true];
        yield return ["/settings/email", true];
        yield return ["/team-members", true];
        yield return ["/team-members/create", true];
        yield return ["/team-members/edit/00000000-0000-0000-0000-000000000001", true];
    }

    [Theory]
    [MemberData(nameof(AllHostRoutes))]
    public void HostRoute_Unauthenticated_DoesNotCrash(string route, bool requiresAuth)
    {
        NavigateAndAssertNoUnhandledError(route);

        if (requiresAuth)
        {
            Assert.Contains("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [MemberData(nameof(AllHostRoutes))]
    public void HostRoute_AsAdmin_DoesNotCrash(string route, bool requiresAuth)
    {
        Require(!string.IsNullOrWhiteSpace(Config.AdminEmail) && !string.IsNullOrWhiteSpace(Config.AdminPassword),
            "Missing env vars: SELENIUM_ADMIN_EMAIL / SELENIUM_ADMIN_PASSWORD");

        Login(Config.AdminEmail!, Config.AdminPassword!);
        NavigateAndAssertNoUnhandledError(route);

        // For authenticated run, regular host routes should not bounce back to login.
        // Some public token routes may still redirect to login/entry depending token validity.
        if (requiresAuth)
        {
            Assert.DoesNotContain("/login", Driver.Url, StringComparison.OrdinalIgnoreCase);
        }
    }
}
