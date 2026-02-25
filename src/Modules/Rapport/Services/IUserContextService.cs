namespace Kuestencode.Rapport.Services;

public interface IUserContextService
{
    Task<Guid?> GetCurrentUserIdAsync();
    Task<string?> GetCurrentUserNameAsync();
    Task<string?> GetCurrentUserRoleAsync();
    Task<bool> IsAdminOrBueroAsync();
}
