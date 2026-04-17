using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class RefundLogRepository : Repository<RefundLog>, IRefundLogRepository<RefundLog>
{
    public RefundLogRepository(FortunoContext context) : base(context) { }

    public async Task<List<RefundLog>> ListByTicketAsync(long ticketId)
        => await _context.RefundLogs.AsNoTracking()
            .Where(x => x.TicketId == ticketId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

    public async Task<List<RefundLog>> InsertBatchAsync(IEnumerable<RefundLog> logs)
    {
        var list = logs.ToList();
        _context.RefundLogs.AddRange(list);
        await _context.SaveChangesAsync();
        return list;
    }
}
