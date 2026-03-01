namespace Kuestencode.Werkbank.Recepta.Domain.Enums;

/// <summary>
/// Hilfsmethoden für DocumentCategory – deutsche Anzeigenamen und EÜR-Gruppen.
/// </summary>
public static class DocumentCategoryExtensions
{
    public static string GetDisplayName(this DocumentCategory category) => category switch
    {
        DocumentCategory.Material => "Material / Waren",
        DocumentCategory.Subcontractor => "Subunternehmer / Fremdleistungen",
        DocumentCategory.Wages => "Löhne & Gehälter",
        DocumentCategory.SocialSecurity => "Sozialversicherung",
        DocumentCategory.Rent => "Miete",
        DocumentCategory.Utilities => "Nebenkosten (Strom, Wasser, Heizung)",
        DocumentCategory.Vehicle => "Kfz-Kosten",
        DocumentCategory.Travel => "Reisekosten",
        DocumentCategory.Marketing => "Marketing & Werbung",
        DocumentCategory.Office => "Büromaterial",
        DocumentCategory.Software => "Software & Lizenzen",
        DocumentCategory.Phone => "Telefon & Internet",
        DocumentCategory.Insurance => "Versicherungen",
        DocumentCategory.Fees => "Beiträge & Gebühren",
        DocumentCategory.Other => "Sonstige Kosten",
        _ => category.ToString()
    };

    public static string GetGroup(this DocumentCategory category) => category switch
    {
        DocumentCategory.Material => "Wareneinsatz",
        DocumentCategory.Subcontractor => "Fremdleistungen",
        DocumentCategory.Wages or DocumentCategory.SocialSecurity => "Personal",
        DocumentCategory.Rent or DocumentCategory.Utilities => "Raum & Betrieb",
        DocumentCategory.Vehicle => "Fahrzeuge",
        DocumentCategory.Travel => "Reisen",
        DocumentCategory.Marketing => "Marketing",
        DocumentCategory.Office or DocumentCategory.Software or DocumentCategory.Phone => "Büro & IT",
        DocumentCategory.Insurance or DocumentCategory.Fees => "Versicherungen & Beiträge",
        _ => "Sonstiges"
    };
}
