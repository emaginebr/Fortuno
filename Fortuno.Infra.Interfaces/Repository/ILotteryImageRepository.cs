namespace Fortuno.Infra.Interfaces.Repository;

public interface ILotteryImageRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> ListByLotteryAsync(long lotteryId);
    Task<int> CountByLotteryAsync(long lotteryId);
}
