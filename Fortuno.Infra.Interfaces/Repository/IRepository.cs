namespace Fortuno.Infra.Interfaces.Repository;

public interface IRepository<TModel> where TModel : class
{
    Task<TModel?> GetByIdAsync(long id);
    Task<List<TModel>> ListAllAsync();
    Task<TModel> InsertAsync(TModel entity);
    Task<TModel> UpdateAsync(TModel entity);
    Task DeleteAsync(long id);
    IQueryable<TModel> Query();
}
