namespace Fortuno.Infra.Interfaces.Repository;

public interface IUserReferrerRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<TModel?> GetByUserIdAsync(long userId);
    Task<TModel?> GetByCodeAsync(string referralCode);
    Task<bool> CodeExistsAsync(string referralCode);
}
