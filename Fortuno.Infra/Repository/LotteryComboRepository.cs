using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class LotteryComboRepository : Repository<LotteryCombo>, ILotteryComboRepository<LotteryCombo>
{
    public LotteryComboRepository(FortunoContext context) : base(context) { }

    public async Task<List<LotteryCombo>> ListByLotteryAsync(long lotteryId)
        => await _context.LotteryCombos.AsNoTracking()
            .Where(x => x.LotteryId == lotteryId)
            .OrderBy(x => x.QuantityStart)
            .ToListAsync();

    public async Task<LotteryCombo?> FindMatchingComboAsync(long lotteryId, int quantity)
        => await _context.LotteryCombos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.LotteryId == lotteryId
                && x.QuantityStart <= quantity
                && x.QuantityEnd >= quantity
                && x.QuantityStart > 0);
}
