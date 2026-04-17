namespace Fortuno.Infra.Interfaces.Repository;

public interface ILotteryRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<TModel?> GetBySlugAsync(string slug);
    Task<List<TModel>> ListByStoreAsync(long storeId);
    Task<bool> SlugExistsAsync(string slug);
}
