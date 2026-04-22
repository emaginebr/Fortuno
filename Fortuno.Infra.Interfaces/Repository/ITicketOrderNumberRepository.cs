namespace Fortuno.Infra.Interfaces.Repository;

public interface ITicketOrderNumberRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<List<TModel>> InsertBatchAsync(IEnumerable<TModel> numbers);
    Task<List<TModel>> ListByOrderIdAsync(long ticketOrderId);
}
