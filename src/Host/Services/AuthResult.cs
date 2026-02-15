using Kuestencode.Werkbank.Host.Models;

namespace Kuestencode.Werkbank.Host.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? Error { get; set; }
    public TeamMember? User { get; set; }
    public int? RemainingAttempts { get; set; }
    public int? LockedForMinutes { get; set; }
}
