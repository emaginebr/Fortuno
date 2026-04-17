using Fortuno.DTO.RaffleAward;

namespace Fortuno.Domain.Interfaces;

public interface IRaffleAwardService
{
    Task<RaffleAwardInfo> CreateAsync(long currentUserId, RaffleAwardInsertInfo dto);
    Task<RaffleAwardInfo> UpdateAsync(long currentUserId, long awardId, RaffleAwardUpdateInfo dto);
    Task DeleteAsync(long currentUserId, long awardId);
    Task<List<RaffleAwardInfo>> ListByRaffleAsync(long raffleId);
}
