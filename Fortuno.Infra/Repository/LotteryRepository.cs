using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class LotteryRepository : Repository<Lottery>, ILotteryRepository<Lottery>
{
    public LotteryRepository(FortunoContext context) : base(context) { }

    public async Task<Lottery?> GetBySlugAsync(string slug)
        => await _context.Lotteries.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == slug);

    public async Task<List<Lottery>> ListByStoreAsync(long storeId)
        => await _context.Lotteries.AsNoTracking().Where(x => x.StoreId == storeId).ToListAsync();

    public async Task<bool> SlugExistsAsync(string slug)
        => await _context.Lotteries.AsNoTracking().AnyAsync(x => x.Slug == slug);
}
