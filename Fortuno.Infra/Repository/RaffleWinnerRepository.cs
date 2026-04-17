using Fortuno.Domain.Enums;
using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class RaffleWinnerRepository : Repository<RaffleWinner>, IRaffleWinnerRepository<RaffleWinner>
{
    public RaffleWinnerRepository(FortunoContext context) : base(context) { }

    public async Task<List<RaffleWinner>> ListByRaffleAsync(long raffleId)
        => await _context.RaffleWinners.AsNoTracking()
            .Where(x => x.RaffleId == raffleId)
            .OrderBy(x => x.Position)
            .ToListAsync();

    public async Task<List<long>> ListTicketIdsAlreadyWonInLotteryAsync(long lotteryId)
    {
        return await (from w in _context.RaffleWinners.AsNoTracking()
                      join r in _context.Raffles.AsNoTracking() on w.RaffleId equals r.RaffleId
                      where r.LotteryId == lotteryId
                         && r.Status == RaffleStatus.Closed
                         && w.TicketId != null
                      select w.TicketId!.Value)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<RaffleWinner>> InsertBatchAsync(IEnumerable<RaffleWinner> winners)
    {
        var list = winners.ToList();
        _context.RaffleWinners.AddRange(list);
        await _context.SaveChangesAsync();
        return list;
    }
}
