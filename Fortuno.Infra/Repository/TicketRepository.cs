using Fortuno.Domain.Enums;
using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class TicketRepository : Repository<Ticket>, ITicketRepository<Ticket>
{
    public TicketRepository(FortunoContext context) : base(context) { }

    public async Task<List<Ticket>> ListByUserAsync(long userId, long? lotteryId = null, long? numberContains = null)
    {
        var q = _context.Tickets.AsNoTracking().Where(x => x.UserId == userId);
        if (lotteryId.HasValue) q = q.Where(x => x.LotteryId == lotteryId.Value);
        if (numberContains.HasValue) q = q.Where(x => x.TicketNumber == numberContains.Value);
        return await q.OrderByDescending(x => x.CreatedAt).ToListAsync();
    }

    public async Task<List<Ticket>> ListByLotteryAsync(long lotteryId)
        => await _context.Tickets.AsNoTracking().Where(x => x.LotteryId == lotteryId).ToListAsync();

    public async Task<List<long>> ListSoldNumbersAsync(long lotteryId)
        => await _context.Tickets.AsNoTracking()
            .Where(x => x.LotteryId == lotteryId)
            .Select(x => x.TicketNumber)
            .ToListAsync();

    public async Task<long> CountSoldAsync(long lotteryId)
        => await _context.Tickets.AsNoTracking().CountAsync(x => x.LotteryId == lotteryId);

    public async Task<Ticket?> GetByLotteryAndNumberAsync(long lotteryId, long ticketNumber)
        => await _context.Tickets.AsNoTracking()
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
        => await _context.Tickets.AsNoTracking().Where(x => x.InvoiceId == invoiceId).ToListAsync();
}
