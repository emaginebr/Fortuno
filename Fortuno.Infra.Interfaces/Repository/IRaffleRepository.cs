namespace Fortuno.Infra.Interfaces.Repository;

public interface IRaffleRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> ListByLotteryAsync(long lotteryId, int? statusValue = null);
    Task<int> CountByLotteryAsync(long lotteryId);
}
