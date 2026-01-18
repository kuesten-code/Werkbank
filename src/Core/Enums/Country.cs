namespace Kuestencode.Core.Enums;

/// <summary>
/// Common countries for address selection.
/// </summary>
public enum Country
{
    Deutschland,
    Oesterreich,
    Schweiz,
    Frankreich,
    Niederlande,
    Belgien,
    Luxemburg,
    Italien,
    Spanien,
    Polen,
    Tschechien,
    Daenemark
}

/// <summary>
/// Extension methods for Country enum.
/// </summary>
public static class CountryExtensions
{
    /// <summary>
    /// Returns the ISO 3166-1 Alpha-2 code for the country.
    /// </summary>
    public static string ToIsoCode(this Country country)
    {
        return country switch
        {
            Country.Deutschland => "DE",
            Country.Oesterreich => "AT",
            Country.Schweiz => "CH",
            Country.Frankreich => "FR",
            Country.Niederlande => "NL",
            Country.Belgien => "BE",
            Country.Luxemburg => "LU",
            Country.Italien => "IT",
            Country.Spanien => "ES",
            Country.Polen => "PL",
            Country.Tschechien => "CZ",
            Country.Daenemark => "DK",
            _ => "DE"
        };
    }

    /// <summary>
    /// Returns the display name for the country.
    /// </summary>
    public static string ToDisplayName(this Country country)
    {
        return country switch
        {
            Country.Deutschland => "Deutschland",
            Country.Oesterreich => "Österreich",
            Country.Schweiz => "Schweiz",
            Country.Frankreich => "Frankreich",
            Country.Niederlande => "Niederlande",
            Country.Belgien => "Belgien",
            Country.Luxemburg => "Luxemburg",
            Country.Italien => "Italien",
            Country.Spanien => "Spanien",
            Country.Polen => "Polen",
            Country.Tschechien => "Tschechien",
            Country.Daenemark => "Dänemark",
            _ => "Deutschland"
        };
    }

    /// <summary>
    /// Parses a country name or code to the Country enum.
    /// </summary>
    public static Country FromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Country.Deutschland;

        return value.ToUpper().Trim() switch
        {
            "DE" or "DEUTSCHLAND" or "GERMANY" => Country.Deutschland,
            "AT" or "ÖSTERREICH" or "OESTERREICH" or "AUSTRIA" => Country.Oesterreich,
            "CH" or "SCHWEIZ" or "SWITZERLAND" => Country.Schweiz,
            "FR" or "FRANKREICH" or "FRANCE" => Country.Frankreich,
            "NL" or "NIEDERLANDE" or "NETHERLANDS" => Country.Niederlande,
            "BE" or "BELGIEN" or "BELGIUM" => Country.Belgien,
            "LU" or "LUXEMBURG" or "LUXEMBOURG" => Country.Luxemburg,
            "IT" or "ITALIEN" or "ITALY" => Country.Italien,
            "ES" or "SPANIEN" or "SPAIN" => Country.Spanien,
            "PL" or "POLEN" or "POLAND" => Country.Polen,
            "CZ" or "TSCHECHIEN" or "CZECHIA" => Country.Tschechien,
            "DK" or "DÄNEMARK" or "DAENEMARK" or "DENMARK" => Country.Daenemark,
            _ => Country.Deutschland
        };
    }
}
