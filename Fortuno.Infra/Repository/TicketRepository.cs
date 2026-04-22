using Fortuno.Domain.Enums;
using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class TicketRepository : Repository<Ticket>, ITicketRepository<Ticket>
{
    public TicketRepository(FortunoContext context) : base(context) { }

    public override async Task<Ticket?> GetByIdAsync(long id)
        => await _context.Tickets.AsNoTracking()
            .Include(x => x.Lottery)
            .FirstOrDefaultAsync(x => x.TicketId == id);

    public async Task<(List<Ticket> Items, long TotalCount)> SearchByUserAsync(
        long userId,
        long? lotteryId = null,
        long? ticketNumber = null,
        string? ticketValue = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var q = _context.Tickets.AsNoTracking().Include(x => x.Lottery).Where(x => x.UserId == userId);
        if (lotteryId.HasValue) q = q.Where(x => x.LotteryId == lotteryId.Value);
        if (ticketNumber.HasValue) q = q.Where(x => x.TicketNumber == ticketNumber.Value);
        if (!string.IsNullOrWhiteSpace(ticketValue)) q = q.Where(x => x.TicketValue == ticketValue);
        if (fromDate.HasValue) q = q.Where(x => x.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(x => x.CreatedAt <= toDate.Value);

        var total = await q.LongCountAsync();
        var items = await q
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Ticket>> ListByLotteryAsync(long lotteryId)
        => await _context.Tickets.AsNoTracking().Include(x => x.Lottery).Where(x => x.LotteryId == lotteryId).ToListAsync();

    public async Task<List<long>> ListSoldNumbersAsync(long lotteryId)
        => await _context.Tickets.AsNoTracking()
            .Where(x => x.LotteryId == lotteryId)
            .Select(x => x.TicketNumber)
            .ToListAsync();

    public async Task<long> CountSoldAsync(long lotteryId)
        => await _context.Tickets.AsNoTracking().CountAsync(x => x.LotteryId == lotteryId);

    public async Task<Ticket?> GetByLotteryAndNumberAsync(long lotteryId, long ticketNumber)
        => await _context.Tickets.AsNoTracking()
            .Include(x => x.Lottery)
            .FirstOrDefaultAsync(x => x.LotteryId == lotteryId && x.TicketNumber == ticketNumber);

    public async Task<List<Ticket>> InsertBatchAsync(IEnumerable<Ticket> tickets)
    {
        var list = tickets.ToList();
        _context.Tickets.AddRange(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task MarkRefundStateAsync(IEnumerable<long> ticketIds, int newStateValue)
    {
        var ids = ticketIds.ToList();
        var newState = (TicketRefundState)newStateValue;
        var tickets = await _context.Tickets.Where(x => ids.Contains(x.TicketId)).ToListAsync();
        foreach (var t in tickets) t.RefundState = newState;
        await _context.SaveChangesAsync();
    }

    public async Task<List<Ticket>> ListByInvoiceAsync(long invoiceId)
        => await _context.Tickets.AsNoTracking().Include(x => x.Lottery).Where(x => x.InvoiceId == invoiceId).ToListAsync();
}
