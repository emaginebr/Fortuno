namespace Fortuno.Domain.Interfaces;

public interface IUserReferrerService
{
    Task<string> GetOrCreateCodeForUserAsync(long userId);
    Task<long?> ResolveReferrerUserIdAsync(string? referralCode);
    Task<string?> GetCodeForUserAsync(long userId);
}
