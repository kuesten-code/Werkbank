namespace Kuestencode.Werkbank.Locatio.Domain.Enums;

public enum EinheitStatus
{
    Vermietet = 0,
    Leer = 1,
    Gekuendigt = 2,

    /// <summary>Gehört anderen Eigentümern, nie uns. Keine Einnahmen, Kosten oder AfA.</summary>
    Fremdeigentum = 3,

    /// <summary>Gehörte uns und wurde verkauft. Der Datensatz bleibt archiviert erhalten.</summary>
    Veraeussert = 4
}

public static class EinheitStatusExtensions
{
    public static string Anzeigename(this EinheitStatus status) => status switch
    {
        EinheitStatus.Vermietet => "vermietet",
        EinheitStatus.Leer => "leer",
        EinheitStatus.Gekuendigt => "gekündigt",
        EinheitStatus.Fremdeigentum => "Fremdeigentum",
        EinheitStatus.Veraeussert => "veräußert",
        _ => status.ToString()
    };
}
