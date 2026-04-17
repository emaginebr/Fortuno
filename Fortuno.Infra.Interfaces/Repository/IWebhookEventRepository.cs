namespace Fortuno.Infra.Interfaces.Repository;

public interface IWebhookEventRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<bool> ExistsAsync(long invoiceId, string eventType);
    Task<TModel> InsertIfNotExistsAsync(TModel evt);
}
