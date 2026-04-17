using Fortuno.DTO.LotteryCombo;

namespace Fortuno.Domain.Interfaces;

public interface ILotteryComboService
{
    Task<LotteryComboInfo> CreateAsync(long currentUserId, LotteryComboInsertInfo dto);
    Task<LotteryComboInfo> UpdateAsync(long currentUserId, long comboId, LotteryComboUpdateInfo dto);
    Task DeleteAsync(long currentUserId, long comboId);
    Task<List<LotteryComboInfo>> ListByLotteryAsync(long lotteryId);
}
