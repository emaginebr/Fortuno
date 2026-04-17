using Fortuno.Domain.Interfaces;
using Fortuno.Infra.Interfaces.AppServices;

namespace Fortuno.Domain.Services;

public class StoreOwnershipGuard : IStoreOwnershipGuard
{
    private readonly IProxyPayAppService _proxyPay;

    public StoreOwnershipGuard(IProxyPayAppService proxyPay)
    {
        _proxyPay = proxyPay;
    }

    public async Task<bool> IsOwnerAsync(long storeId, long userId)
    {
        var store = await _proxyPay.GetStoreAsync(storeId);
        return store is not null && store.OwnerUserId == userId;
    }

    public async Task EnsureOwnershipAsync(long storeId, long userId)
    {
        if (!await IsOwnerAsync(storeId, userId))
            throw new UnauthorizedAccessException($"User {userId} não é proprietário da Store {storeId}.");
    }
}
