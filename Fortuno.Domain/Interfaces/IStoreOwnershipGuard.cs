namespace Fortuno.Domain.Interfaces;

public interface IStoreOwnershipGuard
{
    Task EnsureOwnershipAsync(long storeId, long userId);
    Task<bool> IsOwnerAsync(long storeId, long userId);
}
