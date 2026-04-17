using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class InvoiceReferrerRepository : Repository<InvoiceReferrer>, IInvoiceReferrerRepository<InvoiceReferrer>
{
    public InvoiceReferrerRepository(FortunoContext context) : base(context) { }

    public async Task<InvoiceReferrer?> GetByInvoiceIdAsync(long invoiceId)
        => await _context.InvoiceReferrers.AsNoTracking().FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);

    public async Task<List<InvoiceReferrer>> ListByReferrerAsync(long referrerUserId)
        => await _context.InvoiceReferrers.AsNoTracking()
            .Where(x => x.ReferrerUserId == referrerUserId)
            .ToListAsync();

    public async Task<List<InvoiceReferrer>> ListByLotteryAsync(long lotteryId)
        => await _context.InvoiceReferrers.AsNoTracking()
            .Where(x => x.LotteryId == lotteryId)
            .ToListAsync();
}
