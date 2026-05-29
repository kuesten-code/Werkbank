using Kuestencode.Werkbank.Contracta.Domain.Entities;
using Kuestencode.Werkbank.Contracta.Domain.Enums;

namespace Kuestencode.Werkbank.Contracta.Domain.Services;

public class VertragStatusService
{
    public bool KannAktiviertWerden(Wartungsvertrag vertrag) =>
        vertrag.Status == VertragStatus.Entwurf;

    public bool KannPausiertWerden(Wartungsvertrag vertrag) =>
        vertrag.Status == VertragStatus.Aktiv;

    public bool KannBeendetWerden(Wartungsvertrag vertrag) =>
        vertrag.Status is VertragStatus.Aktiv or VertragStatus.Pausiert;

    public bool KannReaktiviertWerden(Wartungsvertrag vertrag) =>
        vertrag.Status == VertragStatus.Pausiert;

    public void Aktivieren(Wartungsvertrag vertrag)
    {
        if (!KannAktiviertWerden(vertrag))
            throw new InvalidOperationException(
                $"Vertrag im Status '{vertrag.Status}' kann nicht aktiviert werden.");
        vertrag.Status = VertragStatus.Aktiv;
    }

    public void Pausieren(Wartungsvertrag vertrag)
    {
        if (!KannPausiertWerden(vertrag))
            throw new InvalidOperationException(
                $"Vertrag im Status '{vertrag.Status}' kann nicht pausiert werden.");
        vertrag.Status = VertragStatus.Pausiert;
    }

    public void Beenden(Wartungsvertrag vertrag)
    {
        if (!KannBeendetWerden(vertrag))
            throw new InvalidOperationException(
                $"Vertrag im Status '{vertrag.Status}' kann nicht beendet werden.");
        vertrag.Status = VertragStatus.Beendet;
    }

    public void Reaktivieren(Wartungsvertrag vertrag)
    {
        if (!KannReaktiviertWerden(vertrag))
            throw new InvalidOperationException(
                $"Vertrag im Status '{vertrag.Status}' kann nicht reaktiviert werden.");
        vertrag.Status = VertragStatus.Aktiv;
    }
}
