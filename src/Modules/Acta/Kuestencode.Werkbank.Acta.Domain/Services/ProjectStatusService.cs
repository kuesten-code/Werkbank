using Kuestencode.Werkbank.Acta.Domain.Entities;
using Kuestencode.Werkbank.Acta.Domain.Enums;

namespace Kuestencode.Werkbank.Acta.Domain.Services;

/// <summary>
/// Service zur Verwaltung der Statusübergänge eines Projekts.
/// Verwendet die ProjectStateMachine für die Validierung und führt
/// die Statusänderungen mit den entsprechenden Seiteneffekten durch.
/// </summary>
public class ProjectStatusService
{
    /// <summary>
    /// Prüft, ob ein Projekt freigegeben werden kann.
    /// Voraussetzung: Status ist Draft.
    /// </summary>
    public bool KannFreigegebenWerden(Project project)
    {
        return ProjectStateMachine.CanTransition(project.Status, ProjectStatus.Active);
    }

    /// <summary>
    /// Prüft, ob ein Projekt pausiert werden kann.
    /// Voraussetzung: Status ist Active.
    /// </summary>
    public bool KannPausiertWerden(Project project)
    {
        return ProjectStateMachine.CanTransition(project.Status, ProjectStatus.Paused);
    }

    /// <summary>
    /// Prüft, ob ein Projekt fortgesetzt werden kann.
    /// Voraussetzung: Status ist Paused.
    /// </summary>
    public bool KannFortgesetztWerden(Project project)
    {
        return ProjectStateMachine.CanTransition(project.Status, ProjectStatus.Active)
               && project.Status == ProjectStatus.Paused;
    }

    /// <summary>
    /// Prüft, ob ein Projekt abgeschlossen werden kann.
    /// Voraussetzung: Status ist Active.
    /// </summary>
    public bool KannAbgeschlossenWerden(Project project)
    {
        return ProjectStateMachine.CanTransition(project.Status, ProjectStatus.Completed);
    }

    /// <summary>
    /// Prüft, ob ein Projekt reaktiviert werden kann.
    /// Voraussetzung: Status ist Completed.
    /// </summary>
    public bool KannReaktiviertWerden(Project project)
    {
        return ProjectStateMachine.CanTransition(project.Status, ProjectStatus.Active)
               && project.Status == ProjectStatus.Completed;
    }

    /// <summary>
    /// Prüft, ob ein Projekt archiviert werden kann.
    /// Voraussetzung: Status ist Completed.
    /// </summary>
    public bool KannArchiviertWerden(Project project)
    {
        return ProjectStateMachine.CanTransition(project.Status, ProjectStatus.Archived);
    }

    /// <summary>
    /// Prüft, ob ein Projekt bearbeitet werden kann.
    /// </summary>
    public bool KannBearbeitetWerden(Project project)
    {
        return ProjectStateMachine.IsEditable(project.Status);
    }

    /// <summary>
    /// Gibt ein Projekt frei (Draft → Active).
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Freigeben(Project project)
    {
        ValidateTransition(project, ProjectStatus.Active, "freigegeben");
        project.Status = ProjectStatus.Active;
    }

    /// <summary>
    /// Pausiert ein Projekt (Active → Paused).
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Pausieren(Project project)
    {
        ValidateTransition(project, ProjectStatus.Paused, "pausiert");
        project.Status = ProjectStatus.Paused;
    }

    /// <summary>
    /// Setzt ein pausiertes Projekt fort (Paused → Active).
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Fortsetzen(Project project)
    {
        ValidateTransition(project, ProjectStatus.Active, "fortgesetzt");
        project.Status = ProjectStatus.Active;
    }

    /// <summary>
    /// Schließt ein Projekt ab (Active → Completed).
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Abschliessen(Project project)
    {
        ValidateTransition(project, ProjectStatus.Completed, "abgeschlossen");
        project.Status = ProjectStatus.Completed;
        project.CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reaktiviert ein abgeschlossenes Projekt (Completed → Active).
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Reaktivieren(Project project)
    {
        ValidateTransition(project, ProjectStatus.Active, "reaktiviert");
        project.Status = ProjectStatus.Active;
        project.CompletedAt = null;
    }

    /// <summary>
    /// Archiviert ein abgeschlossenes Projekt (Completed → Archived).
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void Archivieren(Project project)
    {
        ValidateTransition(project, ProjectStatus.Archived, "archiviert");
        project.Status = ProjectStatus.Archived;
    }

    /// <summary>
    /// Führt einen beliebigen Statusübergang durch, sofern erlaubt.
    /// </summary>
    /// <exception cref="InvalidOperationException">Wenn der Übergang nicht erlaubt ist.</exception>
    public void TransitionTo(Project project, ProjectStatus targetStatus)
    {
        switch (targetStatus)
        {
            case ProjectStatus.Active when project.Status == ProjectStatus.Draft:
                Freigeben(project);
                break;
            case ProjectStatus.Active when project.Status == ProjectStatus.Paused:
                Fortsetzen(project);
                break;
            case ProjectStatus.Active when project.Status == ProjectStatus.Completed:
                Reaktivieren(project);
                break;
            case ProjectStatus.Paused:
                Pausieren(project);
                break;
            case ProjectStatus.Completed:
                Abschliessen(project);
                break;
            case ProjectStatus.Archived:
                Archivieren(project);
                break;
            default:
                throw new InvalidOperationException(
                    $"Ungültiger Statusübergang von '{project.Status}' nach '{targetStatus}'.");
        }
    }

    /// <summary>
    /// Gibt die erlaubten Folgestatus für ein Projekt zurück.
    /// </summary>
    public IReadOnlyList<(ProjectStatus TargetStatus, string ActionName)> GetVerfuegbareUebergaenge(Project project)
    {
        return ProjectStateMachine.GetAvailableTransitions(project.Status);
    }

    private static void ValidateTransition(Project project, ProjectStatus targetStatus, string actionName)
    {
        if (!ProjectStateMachine.CanTransition(project.Status, targetStatus))
        {
            throw new InvalidOperationException(
                $"Projekt kann nicht {actionName} werden. Aktueller Status: {project.Status}");
        }
    }
}
