using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class LotteryImageRepository : Repository<LotteryImage>, ILotteryImageRepository<LotteryImage>
{
    public LotteryImageRepository(FortunoContext context) : base(context) { }

    public async Task<List<LotteryImage>> ListByLotteryAsync(long lotteryId)
        => await _context.LotteryImages.AsNoTracking()
            .Where(x => x.LotteryId == lotteryId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();

    public async Task<int> CountByLotteryAsync(long lotteryId)
        => await _context.LotteryImages.AsNoTracking().CountAsync(x => x.LotteryId == lotteryId);
}
