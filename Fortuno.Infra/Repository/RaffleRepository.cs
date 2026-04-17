using Fortuno.Domain.Enums;
using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class RaffleRepository : Repository<Raffle>, IRaffleRepository<Raffle>
{
    public RaffleRepository(FortunoContext context) : base(context) { }

    public async Task<List<Raffle>> ListByLotteryAsync(long lotteryId, int? statusValue = null)
    {
        var q = _context.Raffles.AsNoTracking().Where(x => x.LotteryId == lotteryId);
        if (statusValue.HasValue)
        {
            var status = (RaffleStatus)statusValue.Value;
            q = q.Where(x => x.Status == status);
        }
        return await q.OrderBy(x => x.RaffleDatetime).ToListAsync();
    }

    public async Task<int> CountByLotteryAsync(long lotteryId)
        => await _context.Raffles.AsNoTracking().CountAsync(x => x.LotteryId == lotteryId);
}
