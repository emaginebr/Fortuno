using Fortuno.DTO.Raffle;
using Fortuno.DTO.RaffleWinner;

namespace Fortuno.Domain.Interfaces;

public interface IRaffleService
{
    Task<RaffleInfo> CreateAsync(long currentUserId, RaffleInsertInfo dto);
    Task<RaffleInfo> UpdateAsync(long currentUserId, long raffleId, RaffleUpdateInfo dto);
    Task DeleteAsync(long currentUserId, long raffleId);
    Task<List<RaffleInfo>> ListByLotteryAsync(long lotteryId);
    Task<RaffleInfo?> GetByIdAsync(long raffleId);

    Task<List<RaffleWinnerPreviewRow>> PreviewWinnersAsync(long currentUserId, RaffleWinnersPreviewRequest request);
    Task<List<RaffleWinnerInfo>> ConfirmWinnersAsync(long currentUserId, RaffleWinnersPreviewRequest request);
    Task<RaffleInfo> CloseAsync(long currentUserId, long raffleId);
    Task<RaffleInfo> CancelAsync(long currentUserId, long raffleId, RaffleCancelRequest request);
}
