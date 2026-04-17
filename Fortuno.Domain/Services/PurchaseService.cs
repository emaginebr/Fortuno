using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Purchase;
using Fortuno.DTO.Webhook;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class PurchaseService : IPurchaseService
{
    private static readonly TimeSpan ReservationTtl = TimeSpan.FromMinutes(15); // FR-030c

    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly ILotteryComboRepository<LotteryCombo> _comboRepo;
    private readonly ITicketRepository<Ticket> _ticketRepo;
    private readonly INumberReservationRepository<NumberReservation> _reservationRepo;
    private readonly IInvoiceReferrerRepository<InvoiceReferrer> _invoiceReferrerRepo;
    private readonly IWebhookEventRepository<WebhookEvent> _webhookEventRepo;
    private readonly INumberCompositionService _numbers;
    private readonly IUserReferrerService _referrer;
    private readonly IProxyPayAppService _proxyPay;

    public PurchaseService(
        ILotteryRepository<Lottery> lotteryRepo,
        ILotteryComboRepository<LotteryCombo> comboRepo,
        ITicketRepository<Ticket> ticketRepo,
        INumberReservationRepository<NumberReservation> reservationRepo,
        IInvoiceReferrerRepository<InvoiceReferrer> invoiceReferrerRepo,
        IWebhookEventRepository<WebhookEvent> webhookEventRepo,
        INumberCompositionService numbers,
        IUserReferrerService referrer,
        IProxyPayAppService proxyPay)
    {
        _lotteryRepo = lotteryRepo;
        _comboRepo = comboRepo;
        _ticketRepo = ticketRepo;
        _reservationRepo = reservationRepo;
        _invoiceReferrerRepo = invoiceReferrerRepo;
        _webhookEventRepo = webhookEventRepo;
        _numbers = numbers;
        _referrer = referrer;
        _proxyPay = proxyPay;
    }

    public async Task<PurchasePreviewInfo> PreviewAsync(long? currentUserId, PurchasePreviewRequest request)
    {
        var (lottery, preview) = await BuildPreviewAsync(currentUserId, request);
        return preview;
    }

    public async Task<PurchaseConfirmResponse> ConfirmAsync(long currentUserId, PurchaseConfirmRequest request)
    {
        var (lottery, preview) = await BuildPreviewAsync(currentUserId, request);

        // Validações finais antes de criar Invoice no ProxyPay
        if (preview.Quantity <= 0)
            throw new InvalidOperationException("Quantidade inválida.");
        if (preview.Quantity > preview.AvailableTickets)
            throw new InvalidOperationException($"Restam apenas {preview.AvailableTickets} tickets disponíveis.");
        if (request.Mode == PurchaseAssignmentModeDto.UserPicks)
        {
            if (request.PickedNumbers is null || request.PickedNumbers.Count != request.Quantity)
                throw new InvalidOperationException("Em UserPicks, pickedNumbers deve ter a mesma quantidade do pedido.");
            await ValidateUserPickedNumbersAsync(lottery, request.PickedNumbers!);

            // Reserva temporária (TTL 15 min) antes de criar o invoice
            await _reservationRepo.ExpireByUserAndLotteryAsync(currentUserId, lottery.LotteryId);
            var expiresAt = DateTime.UtcNow.Add(ReservationTtl);
            var reservations = request.PickedNumbers!.Select(n => new NumberReservation
            {
                LotteryId = lottery.LotteryId,
                UserId = currentUserId,
                TicketNumber = n,
                ExpiresAt = expiresAt
            });
            await _reservationRepo.InsertBatchAsync(reservations);
        }

        // Cria Invoice no ProxyPay
        var invoice = await _proxyPay.CreateInvoiceAsync(new ProxyPayCreateInvoiceRequest
        {
            StoreId = lottery.StoreId,
            Amount = preview.TotalAmount,
            Description = $"Fortuno Lottery #{lottery.LotteryId} - {preview.Quantity} tickets",
            Metadata = new Dictionary<string, string>
            {
                ["fortunoLotteryId"] = lottery.LotteryId.ToString(),
                ["fortunoUserId"] = currentUserId.ToString(),
                ["fortunoQuantity"] = preview.Quantity.ToString(),
                ["fortunoMode"] = ((int)request.Mode).ToString(),
                ["fortunoReferralCode"] = request.ReferralCode ?? string.Empty
            }
        });

        // Associa reservas UserPicks ao InvoiceId criado
        if (request.Mode == PurchaseAssignmentModeDto.UserPicks)
        {
            var mine = await _reservationRepo.ListByUserAndLotteryAsync(currentUserId, lottery.LotteryId);
            foreach (var r in mine.Where(x => x.InvoiceId is null))
            {
                r.InvoiceId = invoice.InvoiceId;
                await _reservationRepo.UpdateAsync(r);
            }
        }

        return new PurchaseConfirmResponse
        {
            InvoiceId = invoice.InvoiceId,
            PixQrCode = invoice.PixQrCode,
            PixCopyPaste = invoice.PixCopyPaste,
            ExpiresAt = invoice.ExpiresAt,
            TotalAmount = preview.TotalAmount
        };
    }

    public async Task ProcessPaidWebhookAsync(ProxyPayWebhookPayload payload)
    {
        if (payload.EventType != "invoice.paid") return;
        if (!string.Equals(payload.Tenant, "fortuna", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Tenant inválido no webhook.");

        // Idempotência por (InvoiceId, EventType) — FR-029b
        if (await _webhookEventRepo.ExistsAsync(payload.Data.InvoiceId, payload.EventType))
            return;

        await _webhookEventRepo.InsertIfNotExistsAsync(new WebhookEvent
        {
            InvoiceId = payload.Data.InvoiceId,
            EventType = payload.EventType
        });

        var metadata = payload.Data.Metadata ?? new Dictionary<string, string>();
        if (!metadata.TryGetValue("fortunoLotteryId", out var lotteryIdStr) ||
            !long.TryParse(lotteryIdStr, out var lotteryId))
            return;
        if (!metadata.TryGetValue("fortunoUserId", out var userIdStr) ||
            !long.TryParse(userIdStr, out var userId))
            return;
        if (!metadata.TryGetValue("fortunoQuantity", out var qtyStr) ||
            !int.TryParse(qtyStr, out var quantity))
            return;
        if (!metadata.TryGetValue("fortunoMode", out var modeStr) ||
            !int.TryParse(modeStr, out var modeInt))
            modeInt = (int)PurchaseAssignmentMode.Random;
        metadata.TryGetValue("fortunoReferralCode", out var referralCode);

        var lottery = await _lotteryRepo.GetByIdAsync(lotteryId);
        if (lottery is null) return;
        if (lottery.Status != LotteryStatus.Open) return; // recusa se vendas já encerraram

        var mode = (PurchaseAssignmentMode)modeInt;
        var invoiceId = payload.Data.InvoiceId;
        var paidAmount = payload.Data.Amount;

        List<long> finalNumbers;
        if (mode == PurchaseAssignmentMode.UserPicks)
        {
            var reservations = await _reservationRepo.ListByUserAndLotteryAsync(userId, lotteryId);
            var linked = reservations.Where(r => r.InvoiceId == invoiceId).ToList();
            if (linked.Count != quantity)
            {
                // Reserva expirou ou incompleta: recusa emissão, marca pendente de estorno
                return;
            }
            finalNumbers = linked.Select(r => r.TicketNumber).ToList();
            // Expira reservas (remove da lista ativa)
            foreach (var r in linked) r.ExpiresAt = DateTime.UtcNow;
            foreach (var r in linked) await _reservationRepo.UpdateAsync(r);
        }
        else
        {
            finalNumbers = await DrawRandomAvailableNumbersAsync(lottery, quantity);
            if (finalNumbers.Count != quantity) return;
        }

        // Gera Tickets
        var tickets = finalNumbers.Select(n => new Ticket
        {
            LotteryId = lotteryId,
            UserId = userId,
            InvoiceId = invoiceId,
            TicketNumber = n,
            RefundState = TicketRefundState.None
        });
        await _ticketRepo.InsertBatchAsync(tickets);

        // Registra InvoiceReferrer quando aplicável
        if (!string.IsNullOrWhiteSpace(referralCode))
        {
            var referrerId = await _referrer.ResolveReferrerUserIdAsync(referralCode);
            if (referrerId.HasValue && referrerId.Value != userId)
            {
                await _invoiceReferrerRepo.InsertAsync(new InvoiceReferrer
                {
                    InvoiceId = invoiceId,
                    ReferrerUserId = referrerId.Value,
                    LotteryId = lotteryId,
                    ReferralPercentAtPurchase = lottery.ReferralPercent
                });
            }
        }
    }

    // ---------- helpers ----------

    private async Task<(Lottery lottery, PurchasePreviewInfo preview)> BuildPreviewAsync(long? currentUserId, PurchasePreviewRequest request)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(request.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {request.LotteryId} não encontrada.");

        if (lottery.Status != LotteryStatus.Open)
            throw new InvalidOperationException("Compras disponíveis apenas em Lottery Open.");

        if (request.Quantity <= 0)
            throw new InvalidOperationException("Quantidade deve ser maior que zero.");
        if (lottery.TicketMin > 0 && request.Quantity < lottery.TicketMin)
            throw new InvalidOperationException($"Quantidade mínima por compra é {lottery.TicketMin}.");
        if (lottery.TicketMax > 0 && request.Quantity > lottery.TicketMax)
            throw new InvalidOperationException($"Quantidade máxima por compra é {lottery.TicketMax}.");

        var available = await ComputeAvailableAsync(lottery);

        // Combo automático
        var combo = await _comboRepo.FindMatchingComboAsync(lottery.LotteryId, request.Quantity);
        var subtotal = lottery.TicketPrice * request.Quantity;
        decimal discount = 0;
        string? comboName = null;
        if (combo is not null && combo.DiscountValue > 0)
        {
            discount = Math.Round(subtotal * (decimal)(combo.DiscountValue / 100f), 2);
            comboName = combo.Name;
        }
        var total = subtotal - discount;

        // Referrer
        long? referrerUserId = null;
        bool referrerIsSelf = false;
        if (!string.IsNullOrWhiteSpace(request.ReferralCode))
        {
            var resolved = await _referrer.ResolveReferrerUserIdAsync(request.ReferralCode);
            if (resolved.HasValue)
            {
                if (currentUserId.HasValue && resolved.Value == currentUserId.Value)
                    referrerIsSelf = true;
                else
                    referrerUserId = resolved;
            }
        }

        var preview = new PurchasePreviewInfo
        {
            LotteryId = lottery.LotteryId,
            Quantity = request.Quantity,
            UnitPrice = lottery.TicketPrice,
            DiscountValue = discount,
            TotalAmount = total,
            ApplicableCombo = comboName,
            AvailableTickets = available,
            ReferrerUserId = referrerUserId,
            ReferrerIsSelf = referrerIsSelf
        };

        return (lottery, preview);
    }

    private async Task<long> ComputeAvailableAsync(Lottery lottery)
    {
        long poolSize;
        if (lottery.NumberType == NumberType.Int64)
            poolSize = _numbers.CountPossibilities(NumberType.Int64, lottery.NumberValueMin, lottery.NumberValueMax);
        else
            poolSize = Math.Max(0, lottery.TicketNumEnd - lottery.TicketNumIni + 1);

        var sold = await _ticketRepo.CountSoldAsync(lottery.LotteryId);
        var reserved = (await _reservationRepo.ListActiveReservedNumbersAsync(lottery.LotteryId)).Count;
        return Math.Max(0, poolSize - sold - reserved);
    }

    private async Task ValidateUserPickedNumbersAsync(Lottery lottery, IReadOnlyList<long> numbers)
    {
        if (numbers.Distinct().Count() != numbers.Count)
            throw new InvalidOperationException("Números escolhidos duplicados.");

        foreach (var n in numbers)
        {
            if (lottery.NumberType == NumberType.Int64)
            {
                if (n < lottery.NumberValueMin || n > lottery.NumberValueMax)
                    throw new InvalidOperationException($"Número {n} fora da faixa permitida.");
            }
            else
            {
                if (n < lottery.TicketNumIni || (lottery.TicketNumEnd > 0 && n > lottery.TicketNumEnd))
                    throw new InvalidOperationException($"Número {n} fora da faixa de tickets.");
                if (!_numbers.IsValid(lottery.NumberType, n, lottery.NumberValueMin, lottery.NumberValueMax))
                    throw new InvalidOperationException($"Número {n} viola a composição de componentes.");
            }
        }

        if (!await _reservationRepo.AreNumbersAvailableAsync(lottery.LotteryId, numbers))
            throw new InvalidOperationException("Um ou mais números já foram reservados ou vendidos.");
    }

    private async Task<List<long>> DrawRandomAvailableNumbersAsync(Lottery lottery, int quantity)
    {
        var sold = new HashSet<long>(await _ticketRepo.ListSoldNumbersAsync(lottery.LotteryId));
        var reserved = new HashSet<long>(await _reservationRepo.ListActiveReservedNumbersAsync(lottery.LotteryId));

        var rng = Random.Shared;
        var picks = new List<long>();

        if (lottery.NumberType == NumberType.Int64)
        {
            var min = lottery.NumberValueMin;
            var max = lottery.NumberValueMax;
            var poolSize = max - min + 1;
            if (poolSize <= 0) return picks;

            // Para faixas pequenas usamos lista completa; para grandes, random with retry.
            if (poolSize <= 1_000_000)
            {
                var pool = Enumerable.Range(min, poolSize)
                    .Select(v => (long)v)
                    .Where(v => !sold.Contains(v) && !reserved.Contains(v))
                    .ToList();
                Shuffle(pool, rng);
                picks = pool.Take(quantity).ToList();
            }
            else
            {
                int attempts = 0;
                while (picks.Count < quantity && attempts < quantity * 20)
                {
                    var v = (long)rng.NextInt64(min, (long)max + 1);
                    if (sold.Contains(v) || reserved.Contains(v) || picks.Contains(v)) { attempts++; continue; }
                    picks.Add(v);
                }
            }
        }
        else
        {
            // Tipo composto — enumera pool sequencial [TicketNumIni, TicketNumEnd] limitado por validação componente a componente
            var pool = new List<long>();
            for (long v = lottery.TicketNumIni; v <= lottery.TicketNumEnd; v++)
            {
                if (sold.Contains(v) || reserved.Contains(v)) continue;
                if (_numbers.IsValid(lottery.NumberType, v, lottery.NumberValueMin, lottery.NumberValueMax))
                    pool.Add(v);
            }
            Shuffle(pool, rng);
            picks = pool.Take(quantity).ToList();
        }

        return picks;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
