using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

namespace Kuestencode.SeleniumTests;

public abstract class SeleniumTestBase : IDisposable
{
    protected readonly SeleniumConfig Config = SeleniumConfig.Current;
    protected IWebDriver Driver { get; }

    protected SeleniumTestBase()
    {
        var options = new ChromeOptions();
        options.AddArgument("--window-size=1600,1000");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        if (Config.Headless)
        {
            options.AddArgument("--headless=new");
        }

        Driver = new ChromeDriver(options);
    }

    protected WebDriverWait Wait(int seconds = 15) =>
        new(Driver, TimeSpan.FromSeconds(seconds));

    protected void Navigate(string relativePath)
    {
        Driver.Navigate().GoToUrl(new Uri(new Uri(Config.BaseUrl), relativePath));
    }

    protected void NavigateAndAssertNoUnhandledError(string relativePath)
    {
        Navigate(relativePath);

        var wait = Wait();
        wait.Until(d => ((IJavaScriptExecutor)d)
            .ExecuteScript("return document.readyState")
            ?.ToString() == "complete");

        // Blazor unhandled error banner should never appear on a healthy page.
        var pageSource = Driver.PageSource;
        Assert.DoesNotContain("Ein unbehandelter Fehler ist aufgetreten", pageSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unhandled exception has occurred", pageSource, StringComparison.OrdinalIgnoreCase);
    }

    protected void Login(string email, string password)
    {
        Navigate("/login");

        var wait = Wait();
        wait.Until(d => d.FindElements(By.CssSelector("input[type='email']")).Count > 0);

        var emailInput = Driver.FindElement(By.CssSelector("input[type='email']"));
        var passwordInput = Driver.FindElement(By.CssSelector("input[type='password']"));

        emailInput.Clear();
        emailInput.SendKeys(email);

        passwordInput.Clear();
        passwordInput.SendKeys(password);

        var submit = Driver.FindElement(By.CssSelector("button[type='submit']"));
        submit.Click();

        wait.Until(d => !d.Url.Contains("/login", StringComparison.OrdinalIgnoreCase));
    }

    protected HashSet<string> ReadNavLabels()
    {
        var wait = Wait();
        wait.Until(d => d.FindElements(By.CssSelector(".mud-nav-link-text")).Count > 0);

        return Driver.FindElements(By.CssSelector(".mud-nav-link-text"))
            .Select(e => e.Text?.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    protected static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}


