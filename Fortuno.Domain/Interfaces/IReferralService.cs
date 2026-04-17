using Fortuno.DTO.Commission;
using Fortuno.DTO.Referrer;

namespace Fortuno.Domain.Interfaces;

public interface IReferralService
{
    Task<ReferrerEarningsPanel> GetEarningsForReferrerAsync(long userId);
    Task<LotteryCommissionsPanel> GetPayablesForLotteryAsync(long currentUserId, long lotteryId);
}
