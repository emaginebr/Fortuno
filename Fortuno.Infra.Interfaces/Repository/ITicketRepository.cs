namespace Fortuno.Infra.Interfaces.Repository;

public interface ITicketRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<(List<TModel> Items, long TotalCount)> SearchByUserAsync(
        long userId,
        long? lotteryId = null,
        string? ticketValue = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20);
    Task<List<TModel>> ListByLotteryAsync(long lotteryId);
    Task<List<long>> ListSoldNumbersAsync(long lotteryId);
    Task<long> CountSoldAsync(long lotteryId);
    Task<TModel?> GetByLotteryAndNumberAsync(long lotteryId, long ticketNumber);
    Task<List<TModel>> InsertBatchAsync(IEnumerable<TModel> tickets);
    Task MarkRefundStateAsync(IEnumerable<long> ticketIds, int newStateValue);
    Task<List<TModel>> ListByInvoiceAsync(long invoiceId);
}
