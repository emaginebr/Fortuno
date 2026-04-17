namespace Fortuno.Infra.Interfaces.Repository;

public interface IRaffleAwardRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> ListByRaffleAsync(long raffleId);
    Task<int> CountByRaffleAsync(long raffleId);
    Task ReassignToRaffleAsync(long awardId, long newRaffleId);
}
