using Fortuno.DTO.LotteryImage;

namespace Fortuno.Domain.Interfaces;

public interface ILotteryImageService
{
    Task<LotteryImageInfo> CreateAsync(long currentUserId, LotteryImageInsertInfo dto);
    Task<LotteryImageInfo> UpdateAsync(long currentUserId, long imageId, LotteryImageUpdateInfo dto);
    Task DeleteAsync(long currentUserId, long imageId);
    Task<List<LotteryImageInfo>> ListByLotteryAsync(long lotteryId);
}
