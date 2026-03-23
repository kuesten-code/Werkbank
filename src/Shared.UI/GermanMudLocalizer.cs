using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Kuestencode.Shared.UI;

/// <summary>
/// Overrides MudBlazor's built-in English strings with German equivalents.
/// Register via: services.AddMudServices(c => c.PopoverOptions.ThrowOnDuplicateProvider = false)
///               services.AddSingleton&lt;MudLocalizer, GermanMudLocalizer&gt;()
/// </summary>
public class GermanMudLocalizer : MudLocalizer
{
    private static readonly Dictionary<string, string> _translations = new()
    {
        ["MudDataGrid.ColVis"]                        = "Spalten",
        ["MudDataGrid.False"]                         = "Falsch",
        ["MudDataGrid.True"]                          = "Wahr",
        ["MudDataGrid.AddFilter"]                     = "Filter hinzufügen",
        ["MudDataGrid.Apply"]                         = "Anwenden",
        ["MudDataGrid.Cancel"]                        = "Abbrechen",
        ["MudDataGrid.Clear"]                         = "Leeren",
        ["MudDataGrid.CollapseAllGroups"]             = "Alle Gruppen schließen",
        ["MudDataGrid.ExpandAllGroups"]               = "Alle Gruppen öffnen",
        ["MudDataGrid.Filter"]                        = "Filtern",
        ["MudDataGrid.FilterValue"]                   = "Filterwert",
        ["MudDataGrid.Group"]                         = "Gruppieren",
        ["MudDataGrid.Hide"]                          = "Verstecken",
        ["MudDataGrid.is after"]                      = "ist nach",
        ["MudDataGrid.is before"]                     = "ist vor",
        ["MudDataGrid.is empty"]                      = "ist leer",
        ["MudDataGrid.is not empty"]                  = "ist nicht leer",
        ["MudDataGrid.is on or after"]                = "ist am oder nach",
        ["MudDataGrid.is on or before"]               = "ist am oder vor",
        ["MudDataGrid.contains"]                      = "enthält",
        ["MudDataGrid.ends with"]                     = "endet mit",
        ["MudDataGrid.equals"]                        = "gleich",
        ["MudDataGrid.not contains"]                  = "enthält nicht",
        ["MudDataGrid.not equals"]                    = "ungleich",
        ["MudDataGrid.starts with"]                   = "beginnt mit",
        ["MudDataGrid.MoveDown"]                      = "Nach unten",
        ["MudDataGrid.MoveUp"]                        = "Nach oben",
        ["MudDataGrid.RefreshData"]                   = "Daten aktualisieren",
        ["MudDataGrid.Save"]                          = "Speichern",
        ["MudDataGrid.ShowAll"]                       = "Alle anzeigen",
        ["MudDataGrid.Sort"]                          = "Sortieren",
        ["MudDataGrid.Ungroup"]                       = "Gruppierung aufheben",
        ["MudDataGrid.Unsort"]                        = "Sortierung aufheben",
        ["MudDataGrid.Value"]                         = "Wert",
        ["MudDataGrid.Operator"]                      = "Operator",
        ["MudTable.GroupIndentation"]                 = "Gruppeneinrückung",
        ["MudTablePager.RowsPerPage"]                 = "Zeilen pro Seite:",
        ["MudTablePager.AllRows"]                     = "Alle",
        ["MudTablePager.Info"]                        = "{first_item}-{last_item} von {all_items}",
    };

    public override LocalizedString this[string key]
    {
        get
        {
            if (_translations.TryGetValue(key, out var translation))
                return new LocalizedString(key, translation);
            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }
}
