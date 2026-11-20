using Kuestencode.Shared.Contracts.Host;

namespace Kuestencode.Shared.UI.Components.Positionskatalog;

/// <summary>
/// Zustand der Vorschlagsliste unter dem Beschreibungsfeld: Treffer, Markierung per Pfeiltasten, offen/zu.
/// </summary>
public class VorschlagsAuswahl
{
    public IReadOnlyList<PositionsvorlageDto> Treffer { get; private set; } = [];
    public int Markiert { get; private set; } = -1;
    public bool Offen { get; private set; }

    /// <summary>Nur dann gehören Pfeiltasten der Liste statt dem Textfeld.</summary>
    public bool ListeAktiv => Offen && Treffer.Count > 0;
    public bool HatMarkierung => ListeAktiv && Markiert >= 0;
    public PositionsvorlageDto? MarkierterTreffer => HatMarkierung ? Treffer[Markiert] : null;

    public void Anzeigen(IReadOnlyList<PositionsvorlageDto> treffer)
    {
        Treffer = treffer;
        Markiert = -1;
        Offen = true;
    }

    public void Runter()
    {
        if (ListeAktiv)
            Markiert = (Markiert + 1) % Treffer.Count;
    }

    public void Hoch()
    {
        if (ListeAktiv)
            Markiert = Markiert <= 0 ? Treffer.Count - 1 : Markiert - 1;
    }

    public void Schliessen()
    {
        Treffer = [];
        Markiert = -1;
        Offen = false;
    }
}
