namespace Fortuno.Infra.Interfaces.Repository;

public interface ITicketOrderRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<TModel?> GetByInvoiceIdAsync(long invoiceId);
    Task<int> TryMarkPaidAsync(long purchaseIntentId);
    Task<int> TryMarkExpiredAsync(long purchaseIntentId);
    Task<int> TryMarkCancelledAsync(long purchaseIntentId);
}
