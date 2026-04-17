namespace Fortuno.Infra.Interfaces.AppServices;

public interface INAuthAppService
{
    Task<NAuthUserInfo?> GetByIdAsync(long userId);
    Task<List<NAuthUserInfo>> GetByIdsAsync(IEnumerable<long> userIds);
    Task<NAuthUserInfo?> GetCurrentAsync();
}

public class NAuthUserInfo
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
