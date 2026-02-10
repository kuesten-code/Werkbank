using System.Linq;

namespace Kuestencode.Werkbank.Acta.Models;

/// <summary>
/// Minimal representation of a team member for task assignments.
/// </summary>
public class TeamMember
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>
    /// Returns up to two uppercase initials for avatar rendering.
    /// </summary>
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
            {
                return "?";
            }

            var parts = DisplayName
                .Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0]));

            return new string(parts.DefaultIfEmpty('?').ToArray());
        }
    }
}
