namespace Kuestencode.SeleniumTests;

public sealed class SeleniumConfig
{
    public string BaseUrl { get; } = ReadString("SELENIUM_BASE_URL", "http://localhost:8080");
    public bool Headless { get; } = ReadBool("SELENIUM_HEADLESS", true);

    public string? AdminEmail { get; } = ReadOptional("SELENIUM_ADMIN_EMAIL");
    public string? AdminPassword { get; } = ReadOptional("SELENIUM_ADMIN_PASSWORD");

    public string? BueroEmail { get; } = ReadOptional("SELENIUM_BUERO_EMAIL");
    public string? BueroPassword { get; } = ReadOptional("SELENIUM_BUERO_PASSWORD");

    public string? MitarbeiterEmail { get; } = ReadOptional("SELENIUM_MITARBEITER_EMAIL");
    public string? MitarbeiterPassword { get; } = ReadOptional("SELENIUM_MITARBEITER_PASSWORD");

    public static SeleniumConfig Current { get; } = new();

    private static string ReadString(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? ReadOptional(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool ReadBool(string key, bool fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }
}
