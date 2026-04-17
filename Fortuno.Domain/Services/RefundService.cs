using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Refund;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class RefundService : IRefundService
{
    private readonly ITicketRepository<Ticket> _ticketRepo;
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly IRefundLogRepository<RefundLog> _logRepo;
    private readonly IStoreOwnershipGuard _ownership;

    public RefundService(
        ITicketRepository<Ticket> ticketRepo,
        ILotteryRepository<Lottery> lotteryRepo,
        IRefundLogRepository<RefundLog> logRepo,
        IStoreOwnershipGuard ownership)
    {
        _ticketRepo = ticketRepo;
        _lotteryRepo = lotteryRepo;
        _logRepo = logRepo;
        _ownership = ownership;
    }

    public async Task<List<PendingRefundTicketInfo>> ListPendingByLotteryAsync(long currentUserId, long lotteryId)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");
        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        var tickets = await _ticketRepo.ListByLotteryAsync(lotteryId);
        return tickets
            .Where(t => t.RefundState == TicketRefundState.PendingRefund)
            .Select(t => new PendingRefundTicketInfo
            {
                TicketId = t.TicketId,
                LotteryId = t.LotteryId,
                TicketNumber = t.TicketNumber,
                UserId = t.UserId,
                InvoiceId = t.InvoiceId,
                RefundState = (TicketRefundStateDto)t.RefundState,
                CreatedAt = t.CreatedAt
            })
            .ToList();
    }

    public async Task<int> MarkRefundedAsync(long currentUserId, RefundStatusChangeRequest request)
    {
        if (request.TicketIds.Count == 0)
            throw new InvalidOperationException("Informe ao menos um ticketId.");

        // Todos os tickets devem pertencer à mesma Lottery; o dono dessa Lottery deve ser o currentUserId
        var tickets = new List<Ticket>();
        foreach (var id in request.TicketIds.Distinct())
        {
            var t = await _ticketRepo.GetByIdAsync(id);
            if (t is null) continue;
            tickets.Add(t);
        }
        if (tickets.Count == 0)
            throw new InvalidOperationException("Nenhum ticket válido encontrado.");

        var lotteryIds = tickets.Select(t => t.LotteryId).Distinct().ToList();
        foreach (var lid in lotteryIds)
        {
            var lottery = await _lotteryRepo.GetByIdAsync(lid)
                ?? throw new KeyNotFoundException($"Lottery {lid} não encontrada.");
            await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);
        }

        var eligible = tickets.Where(t => t.RefundState == TicketRefundState.PendingRefund).ToList();
        if (eligible.Count == 0) return 0;

        var now = DateTime.UtcNow;
        await _ticketRepo.MarkRefundStateAsync(eligible.Select(t => t.TicketId), (int)TicketRefundState.Refunded);

        // Registra logs de auditoria (FR-033c)
        var logs = new List<RefundLog>();
        foreach (var t in eligible)
        {
            var lottery = await _lotteryRepo.GetByIdAsync(t.LotteryId);
            logs.Add(new RefundLog
            {
                TicketId = t.TicketId,
                ExecutedByUserId = currentUserId,
                ReferenceValue = lottery?.TicketPrice ?? 0,
                ExternalReference = request.ExternalReference,
                CreatedAt = now
            });
        }
        await _logRepo.InsertBatchAsync(logs);

        return eligible.Count;
    }
}
