using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class RaffleAwardRepository : Repository<RaffleAward>, IRaffleAwardRepository<RaffleAward>
{
    public RaffleAwardRepository(FortunoContext context) : base(context) { }

    public async Task<List<RaffleAward>> ListByRaffleAsync(long raffleId)
        => await _context.RaffleAwards.AsNoTracking()
            .Where(x => x.RaffleId == raffleId)
            .OrderBy(x => x.Position)
            .ToListAsync();

    public async Task<int> CountByRaffleAsync(long raffleId)
        => await _context.RaffleAwards.AsNoTracking().CountAsync(x => x.RaffleId == raffleId);

    public async Task ReassignToRaffleAsync(long awardId, long newRaffleId)
    {
        var award = await _context.RaffleAwards.FindAsync(awardId)
            ?? throw new KeyNotFoundException($"RaffleAward {awardId} não encontrado.");
        award.RaffleId = newRaffleId;
        await _context.SaveChangesAsync();
    }
}
