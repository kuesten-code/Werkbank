using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Werkbank.Host.Services;

/// <summary>
/// Plausibilitätsprüfung einer Modul-Registrierung: der Host ruft nur die für das Modul konfigurierte
/// Adresse ab, und Navigation/Icons dürfen nur auf eigene Pfade zeigen (kein Phishing-Link, keine fremden Ressourcen).
/// </summary>
public static class ModuleRegistrationValidator
{
    public static string? Validate(ModuleInfoDto moduleInfo, IConfiguration configuration)
    {
        var serviceUrl = configuration[$"ServiceUrls:{moduleInfo.ModuleName}"];
        if (string.IsNullOrWhiteSpace(serviceUrl) || !Uri.TryCreate(serviceUrl, UriKind.Absolute, out var serviceUri))
            return $"Für Modul '{moduleInfo.ModuleName}' ist keine ServiceUrl konfiguriert.";

        if (moduleInfo.HealthCheckUrl != null && !PointsTo(moduleInfo.HealthCheckUrl, serviceUri))
            return $"HealthCheckUrl muss auf {serviceUri.GetLeftPart(UriPartial.Authority)} zeigen.";

        if (!IsLocalPath(moduleInfo.LogoUrl))
            return "LogoUrl muss ein relativer Pfad sein.";

        var invalidItem = Flatten(moduleInfo.NavigationItems)
            .FirstOrDefault(item => !IsLocalPath(item.Href) || !IsLocalPath(item.Icon));
        return invalidItem == null ? null : $"Navigationseintrag '{invalidItem.Label}' verweist nicht auf einen relativen Pfad.";
    }

    private static bool PointsTo(string url, Uri serviceUri) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && Uri.Compare(uri, serviceUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0;

    private static bool IsLocalPath(string? path) =>
        string.IsNullOrEmpty(path)
        || (path.StartsWith('/') && !path.StartsWith("//") && !path.StartsWith("/\\"));

    private static IEnumerable<NavItemDto> Flatten(IEnumerable<NavItemDto> items) =>
        items.SelectMany(item => Flatten(item.Children).Prepend(item));
}
