namespace Fortuno.Infra.Interfaces.Repository;

public interface IRefundLogRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> ListByTicketAsync(long ticketId);
    Task<List<TModel>> InsertBatchAsync(IEnumerable<TModel> logs);
}
