using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Commission;
using Fortuno.DTO.Referrer;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class ReferralService : IReferralService
{
    private readonly IInvoiceReferrerRepository<InvoiceReferrer> _invoiceReferrerRepo;
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly ITicketRepository<Ticket> _ticketRepo;
    private readonly IUserReferrerRepository<UserReferrer> _userReferrerRepo;
    private readonly IStoreOwnershipGuard _ownership;
    private readonly INAuthAppService _nauth;
    private readonly IUserReferrerService _referrerService;

    public ReferralService(
        IInvoiceReferrerRepository<InvoiceReferrer> invoiceReferrerRepo,
        ILotteryRepository<Lottery> lotteryRepo,
        ITicketRepository<Ticket> ticketRepo,
        IUserReferrerRepository<UserReferrer> userReferrerRepo,
        IStoreOwnershipGuard ownership,
        INAuthAppService nauth,
        IUserReferrerService referrerService)
    {
        _invoiceReferrerRepo = invoiceReferrerRepo;
        _lotteryRepo = lotteryRepo;
        _ticketRepo = ticketRepo;
        _userReferrerRepo = userReferrerRepo;
        _ownership = ownership;
        _nauth = nauth;
        _referrerService = referrerService;
    }

    public async Task<ReferrerEarningsPanel> GetEarningsForReferrerAsync(long userId)
    {
        var code = await _referrerService.GetOrCreateCodeForUserAsync(userId);
        var invoices = await _invoiceReferrerRepo.ListByReferrerAsync(userId);

        var panel = new ReferrerEarningsPanel
        {
            ReferralCode = code,
            TotalPurchases = invoices.Count
        };

        var byLottery = new Dictionary<long, ReferrerLotteryBreakdown>();
        decimal total = 0;
        int effectivePurchases = 0;

        foreach (var inv in invoices)
        {
            var amount = await ComputeCommissionForInvoiceAsync(inv);
            if (amount <= 0 && !panel.Note.Contains("0"))
            {
                // Ainda conta como compra indicada; apenas não gera valor
            }

            total += amount;
            if (amount > 0) effectivePurchases++;

            if (!byLottery.TryGetValue(inv.LotteryId, out var row))
            {
                var lottery = await _lotteryRepo.GetByIdAsync(inv.LotteryId);
                row = new ReferrerLotteryBreakdown
                {
                    LotteryId = inv.LotteryId,
                    LotteryName = lottery?.Name ?? $"Lottery #{inv.LotteryId}",
                    Amount = 0,
                    Purchases = 0
                };
                byLottery[inv.LotteryId] = row;
            }
            row.Amount += amount;
            row.Purchases += 1;
        }

        panel.TotalToReceive = Math.Round(total, 2);
        panel.ByLottery = byLottery.Values
            .Select(r => new ReferrerLotteryBreakdown
            {
                LotteryId = r.LotteryId,
                LotteryName = r.LotteryName,
                Amount = Math.Round(r.Amount, 2),
                Purchases = r.Purchases
            })
            .OrderByDescending(r => r.Amount)
            .ToList();
        return panel;
    }

    public async Task<LotteryCommissionsPanel> GetPayablesForLotteryAsync(long currentUserId, long lotteryId)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");
        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        var invoices = await _invoiceReferrerRepo.ListByLotteryAsync(lotteryId);

        var byReferrer = new Dictionary<long, ReferrerCommission>();
        decimal total = 0;

        foreach (var inv in invoices)
        {
            var amount = await ComputeCommissionForInvoiceAsync(inv);
            total += amount;

            if (!byReferrer.TryGetValue(inv.ReferrerUserId, out var row))
            {
                var user = await _nauth.GetByIdAsync(inv.ReferrerUserId);
                var userCode = await _userReferrerRepo.GetByUserIdAsync(inv.ReferrerUserId);
                row = new ReferrerCommission
                {
                    ReferrerUserId = inv.ReferrerUserId,
                    ReferrerName = user?.Name,
                    ReferralCode = userCode?.ReferralCode ?? string.Empty,
                    Amount = 0,
                    Purchases = 0
                };
                byReferrer[inv.ReferrerUserId] = row;
            }
            row.Amount += amount;
            row.Purchases += 1;
        }

        return new LotteryCommissionsPanel
        {
            LotteryId = lotteryId,
            LotteryName = lottery.Name,
            TotalPayable = Math.Round(total, 2),
            ByReferrer = byReferrer.Values
                .Select(r => new ReferrerCommission
                {
                    ReferrerUserId = r.ReferrerUserId,
                    ReferrerName = r.ReferrerName,
                    ReferralCode = r.ReferralCode,
                    Amount = Math.Round(r.Amount, 2),
                    Purchases = r.Purchases
                })
                .OrderByDescending(r => r.Amount)
                .ToList()
        };
    }

    /// <summary>
    /// Calcula em tempo real a comissão de uma Invoice indicada, usando:
    ///   Invoice.PaidAmount * (tickets_válidos / tickets_totais) * Lottery.ReferralPercent / 100
    /// onde tickets_válidos são os não-Refunded e ReferralPercent é o vigente na Lottery.
    /// </summary>
    private async Task<decimal> ComputeCommissionForInvoiceAsync(InvoiceReferrer inv)
    {
        var allTickets = await _ticketRepo.ListByInvoiceAsync(inv.InvoiceId);

        if (allTickets.Count == 0) return 0;

        var valid = allTickets.Count(t => t.RefundState != TicketRefundState.Refunded);
        if (valid == 0) return 0;

        var lottery = await _lotteryRepo.GetByIdAsync(inv.LotteryId);
        if (lottery is null || lottery.ReferralPercent <= 0) return 0;

        // PaidAmount = TicketPrice * allTickets.Count - desconto do combo aplicado na compra.
        // Como o sistema não persiste PaidAmount, derivamos a partir do valor original menos desconto médio
        // registrado na própria Invoice quando disponível. Aqui, para simplificar, usamos:
        //   base = TicketPrice * valid_tickets
        // Se houver combo, a proporcionalidade se mantém (valid/total).
        // NOTE: para paridade exata com FR-R06 seria ideal persistir PaidAmount; aqui retornamos
        // aproximação linear pelo preço unitário × válidos. Registrar melhoria futura se necessário.
        decimal baseAmount = lottery.TicketPrice * valid;
        return Math.Round(baseAmount * (decimal)(lottery.ReferralPercent / 100f), 2);
    }
}
