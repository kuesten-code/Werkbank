namespace Kuestencode.Werkbank.Host.Services;

public interface ISetupService
{
    /// <summary>
    /// Checks if the initial setup is required
    /// </summary>
    Task<bool> IsSetupRequiredAsync();

    /// <summary>
    /// Checks if the setup has been completed
    /// </summary>
    Task<bool> IsSetupCompletedAsync();

    /// <summary>
    /// Completes the initial setup by creating the admin user and settings
    /// </summary>
    Task CompleteSetupAsync(SetupData setupData);
}

public class SetupData
{
    public string AdminName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string? BaseUrl { get; set; }
    public bool LocalOnly { get; set; }
    public bool AuthEnabled { get; set; } = true;
}
