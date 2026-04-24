using Fortuno.DTO.Lottery;

namespace Fortuno.Domain.Interfaces;

public interface ILotteryService
{
    Task<LotteryInfo> CreateAsync(long currentUserId, LotteryInsertInfo dto);
    Task<LotteryInfo> UpdateAsync(long currentUserId, long lotteryId, LotteryUpdateInfo dto);
    Task<LotteryInfo?> GetByIdAsync(long lotteryId);
    Task<LotteryInfo?> GetBySlugAsync(string slug);
    Task<List<LotteryInfo>> ListByStoreAsync(long storeId);
    Task<List<LotteryInfo>> ListMineAsync();
    Task<List<LotteryInfo>> ListOpenAsync();
    Task<LotteryInfo> PublishAsync(long currentUserId, long lotteryId);
    Task<LotteryInfo> RevertToDraftAsync(long currentUserId, long lotteryId);
    Task<LotteryInfo> CloseAsync(long currentUserId, long lotteryId);
    Task<LotteryInfo> CancelAsync(long currentUserId, long lotteryId, LotteryCancelRequest request);
    Task DeleteAsync(long currentUserId, long lotteryId);
    Task<long> CalculatePossibilitiesAsync(long lotteryId);
}
