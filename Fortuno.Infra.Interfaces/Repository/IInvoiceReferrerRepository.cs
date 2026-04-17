namespace Fortuno.Infra.Interfaces.Repository;

public interface IInvoiceReferrerRepository<TModel> : IRepository<TModel> where TModel : class
{
    Task<TModel?> GetByInvoiceIdAsync(long invoiceId);
    Task<List<TModel>> ListByReferrerAsync(long referrerUserId);
    Task<List<TModel>> ListByLotteryAsync(long lotteryId);
}
