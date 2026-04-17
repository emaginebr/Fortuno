namespace Fortuno.Infra.Interfaces.Repository;

public interface ILotteryComboRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> ListByLotteryAsync(long lotteryId);
    Task<TModel?> FindMatchingComboAsync(long lotteryId, int quantity);
}
