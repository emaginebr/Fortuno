using Fortuno.Domain.Enums;
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

    public async Task<Lottery?> GetByIdWithDetailsAsync(long id)
        => await _context.Lotteries
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Combos)
            .Include(x => x.Raffles)
                .ThenInclude(r => r.Awards)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.LotteryId == id);

    public async Task<List<Lottery>> ListByStoreAsync(long storeId)
        => await _context.Lotteries.AsNoTracking().Where(x => x.StoreId == storeId).ToListAsync();

    public async Task<List<Lottery>> ListOpenAsync()
        => await _context.Lotteries
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Combos)
            .Include(x => x.Raffles)
                .ThenInclude(r => r.Awards)
            .AsSplitQuery()
            .Where(x => x.Status == LotteryStatus.Open)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

    public async Task<bool> SlugExistsAsync(string slug)
        => await _context.Lotteries.AsNoTracking().AnyAsync(x => x.Slug == slug);
}
