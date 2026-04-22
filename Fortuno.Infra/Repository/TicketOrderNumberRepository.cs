using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class TicketOrderNumberRepository : Repository<TicketOrderNumber>, ITicketOrderNumberRepository<TicketOrderNumber>
{
    public TicketOrderNumberRepository(FortunoContext context) : base(context) { }

    public async Task<List<TicketOrderNumber>> InsertBatchAsync(IEnumerable<TicketOrderNumber> numbers)
    {
        var list = numbers.ToList();
        if (list.Count == 0) return list;
        await _context.Set<TicketOrderNumber>().AddRangeAsync(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<List<TicketOrderNumber>> ListByOrderIdAsync(long ticketOrderId)
        => await _context.Set<TicketOrderNumber>()
            .AsNoTracking()
            .Where(x => x.TicketOrderId == ticketOrderId)
            .OrderBy(x => x.TicketNumber)
            .ToListAsync();
}
