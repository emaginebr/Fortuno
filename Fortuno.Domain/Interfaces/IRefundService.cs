using Fortuno.DTO.Refund;

namespace Fortuno.Domain.Interfaces;

public interface IRefundService
{
    Task<List<PendingRefundTicketInfo>> ListPendingByLotteryAsync(long currentUserId, long lotteryId);
    Task<int> MarkRefundedAsync(long currentUserId, RefundStatusChangeRequest request);
}
