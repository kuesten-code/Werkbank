using Xunit;

namespace Kuestencode.SeleniumTests;

public class LoginPageTests : SeleniumTestBase
{
    [Fact]
    public void LoginPage_ShowsEmailFieldAndSubmitButton()
    {
        Navigate("/login");

        var wait = Wait();
        wait.Until(d => d.FindElements(OpenQA.Selenium.By.CssSelector("input[type='email']")).Count > 0);

        Assert.NotEmpty(Driver.FindElements(OpenQA.Selenium.By.CssSelector("input[type='email']")));
        Assert.NotEmpty(Driver.FindElements(OpenQA.Selenium.By.CssSelector("button[type='submit']")));
    }
}
