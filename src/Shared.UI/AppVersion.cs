namespace Kuestencode.Shared.UI;

/// <summary>
/// Provides a cache-busting version string that changes on every new deployment.
/// Used as a query parameter on static asset URLs in _Host.cshtml files.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// A unique token generated once per process start. Forces browsers to reload
    /// cached JS/CSS files after a container update.
    /// </summary>
    public static readonly string Token = Guid.NewGuid().ToString("N")[..8];
}
