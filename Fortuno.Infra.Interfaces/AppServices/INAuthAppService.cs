using Fortuno.DTO.NAuth;

namespace Fortuno.Infra.Interfaces.AppServices;

public interface INAuthAppService
{
    Task<NAuthUserInfo?> GetByIdAsync(long userId);
    Task<List<NAuthUserInfo>> GetByIdsAsync(IEnumerable<long> userIds);
    Task<NAuthUserInfo?> GetCurrentAsync();
}
