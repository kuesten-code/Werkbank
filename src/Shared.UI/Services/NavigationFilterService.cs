using Kuestencode.Shared.Contracts.Host;
using Kuestencode.Shared.Contracts.Navigation;

namespace Kuestencode.Shared.UI.Services;

public static class NavigationFilterService
{
    public static List<NavItemDto> FilterNavigationByRole(List<NavItemDto> items, UserRole currentRole)
    {
        var filtered = new List<NavItemDto>();

        foreach (var item in items)
        {
            if (item.Type == NavItemType.Divider)
            {
                filtered.Add(item);
                continue;
            }

            // Check AllowedRoles if specified
            if (item.AllowedRoles != null && item.AllowedRoles.Count > 0)
            {
                if (!item.AllowedRoles.Contains(currentRole))
                {
                    continue; // User doesn't have required role
                }
            }

            // Filter children recursively if item has children
            if (item.Children != null && item.Children.Count > 0)
            {
                var filteredChildren = FilterNavigationByRole(item.Children, currentRole);
                if (filteredChildren.Count > 0)
                {
                    // Create new item with filtered children
                    var itemWithFilteredChildren = item with { Children = filteredChildren };
                    filtered.Add(itemWithFilteredChildren);
                }
            }
            else
            {
                filtered.Add(item);
            }
        }

        // Aufeinanderfolgende Divider entfernen
        return CleanDividers(filtered);
    }

    private static List<NavItemDto> CleanDividers(List<NavItemDto> items)
    {
        var result = new List<NavItemDto>();
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Type == NavItemType.Divider)
            {
                // Kein Divider am Anfang/Ende oder doppelte Divider
                if (result.Count == 0 || result[^1].Type == NavItemType.Divider)
                    continue;
                if (i == items.Count - 1)
                    continue;
            }
            result.Add(items[i]);
        }
        return result;
    }
}
