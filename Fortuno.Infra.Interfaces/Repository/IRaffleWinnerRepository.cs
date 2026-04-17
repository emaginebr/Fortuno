namespace Fortuno.Infra.Interfaces.Repository;

public interface IRaffleWinnerRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> ListByRaffleAsync(long raffleId);
    Task<List<long>> ListTicketIdsAlreadyWonInLotteryAsync(long lotteryId);
    Task<List<TModel>> InsertBatchAsync(IEnumerable<TModel> winners);
}
