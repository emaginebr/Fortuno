namespace Fortuno.Infra.Interfaces.Repository;

public interface ILotteryRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<TModel?> GetBySlugAsync(string slug);
    Task<TModel?> GetByIdWithDetailsAsync(long id);
    Task<List<TModel>> ListByStoreAsync(long storeId);
    Task<List<TModel>> ListOpenAsync();
    Task<bool> SlugExistsAsync(string slug);
}
