namespace Kuestencode.Core.Interfaces;

/// <summary>
/// Represents a project that can be linked to time entries.
/// </summary>
public interface IProject
{
    int Id { get; }
    string Name { get; }
    string? ProjectNumber { get; }
    int CustomerId { get; }
    string CustomerName { get; }
}
