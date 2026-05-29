using Kuestencode.Werkbank.Contracta.Services.Interfaces;

namespace Kuestencode.Werkbank.Contracta.Services.Implementation;

/// <summary>
/// Prüft ob optionale Module (z.B. Faktura) verfügbar sind,
/// indem geprüft wird ob eine ServiceUrl konfiguriert ist.
/// </summary>
public class ModulVerfuegbarkeit : IModulVerfuegbarkeit
{
    public ModulVerfuegbarkeit(IConfiguration configuration)
    {
        IstFakturaVerfuegbar = !string.IsNullOrWhiteSpace(
            configuration.GetValue<string>("ServiceUrls:Faktura"));
    }

    public bool IstFakturaVerfuegbar { get; }
}
