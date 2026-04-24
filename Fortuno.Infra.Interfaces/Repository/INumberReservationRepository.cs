namespace Fortuno.Infra.Interfaces.Repository;

public interface INumberReservationRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<long>> ListActiveReservedNumbersAsync(long lotteryId);
    Task<List<TModel>> ListByUserAndLotteryAsync(long userId, long lotteryId);
    Task<bool> IsNumberReservedAsync(long lotteryId, long ticketNumber);
    Task<bool> AreNumbersAvailableAsync(long lotteryId, IEnumerable<long> numbers);
    Task<List<TModel>> InsertBatchAsync(IEnumerable<TModel> reservations);
    Task ExpireByUserAndLotteryAsync(long userId, long lotteryId);
}
