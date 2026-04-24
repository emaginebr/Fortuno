using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Common;
using Fortuno.DTO.Enums;
using Fortuno.DTO.ProxyPay;
using Fortuno.DTO.Ticket;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using TicketOrderMode = Fortuno.Domain.Enums.TicketOrderMode;

namespace Fortuno.Domain.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository<Ticket> _tickets;
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly ILotteryComboRepository<LotteryCombo> _comboRepo;
    private readonly INumberReservationRepository<NumberReservation> _reservationRepo;
    private readonly ITicketOrderRepository<TicketOrder> _orderRepo;
    private readonly ITicketOrderNumberRepository<TicketOrderNumber> _orderNumberRepo;
    private readonly IInvoiceReferrerRepository<InvoiceReferrer> _invoiceReferrerRepo;
    private readonly INumberCompositionService _numbers;
    private readonly IUserReferrerService _referrer;
    private readonly IProxyPayAppService _proxyPay;
    private readonly INAuthAppService _nauth;

    public TicketService(
        ITicketRepository<Ticket> tickets,
        ILotteryRepository<Lottery> lotteryRepo,
        ILotteryComboRepository<LotteryCombo> comboRepo,
        INumberReservationRepository<NumberReservation> reservationRepo,
        ITicketOrderRepository<TicketOrder> orderRepo,
        ITicketOrderNumberRepository<TicketOrderNumber> orderNumberRepo,
        IInvoiceReferrerRepository<InvoiceReferrer> invoiceReferrerRepo,
        INumberCompositionService numbers,
        IUserReferrerService referrer,
        IProxyPayAppService proxyPay,
        INAuthAppService nauth)
    {
        _tickets = tickets;
        _lotteryRepo = lotteryRepo;
        _comboRepo = comboRepo;
        _reservationRepo = reservationRepo;
        _orderRepo = orderRepo;
        _orderNumberRepo = orderNumberRepo;
        _invoiceReferrerRepo = invoiceReferrerRepo;
        _numbers = numbers;
        _referrer = referrer;
        _proxyPay = proxyPay;
        _nauth = nauth;
    }

    public async Task<PagedResult<TicketInfo>> ListForUserAsync(long userId, TicketSearchQuery query)
    {
        // Normaliza `number`: Int64 → decimal direto; Composed → ordena componentes
        // ascendente e zero-pad cada um para casar com `ticket_value` armazenado.
        // Matching vai sempre contra `ticket_value` (coluna string canônica).
        var normalized = NormalizeNumberFilter(query.Number);

        var (items, total) = await _tickets.SearchByUserAsync(
            userId,
            query.LotteryId,
            normalized,
            query.FromDate,
            query.ToDate,
            query.Page,
            query.PageSize);

        return new PagedResult<TicketInfo>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = query.Page < 1 ? 1 : query.Page,
            PageSize = query.PageSize < 1 ? 20 : query.PageSize,
            TotalCount = total
        };
    }

    // Normaliza filtro textual para a forma canônica armazenada em `ticket_value`.
    // "42"            → "42"
    // "60-39-05-28-11"→ "05-11-28-39-60" (ordenado, zero-padded 2 dígitos)
    // Se o input for inválido, retorna a própria string (match vai falhar mas
    // não propaga erro de validação — é filtro opcional).
    private static string? NormalizeNumberFilter(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var text = input.Trim();
        if (!text.Contains('-')) return text;

        var parts = text.Split('-');
        var parsed = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out var c) || c < 0 || c > 99)
                return text; // deixa passar — match falhará no banco
            parsed[i] = c;
        }
        Array.Sort(parsed);
        return string.Join("-", parsed.Select(c => c.ToString("D2")));
    }

    public async Task<TicketInfo?> GetByIdAsync(long ticketId, long currentUserId)
    {
        var t = await _tickets.GetByIdAsync(ticketId);
        if (t is null || t.UserId != currentUserId) return null;
        return MapToDto(t);
    }

    // ---------- POST /tickets/qrcode ----------

    public async Task<TicketQRCodeInfo> CreateQRCodeAsync(long currentUserId, TicketOrderRequest request)
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

        // Pool check upfront — evita chamar ProxyPay / criar reservas se
        // já não há números suficientes antes mesmo da compra.
        var pool = await ComputePoolStatsAsync(lottery);
        if (pool.Available < request.Quantity)
            throw new InvalidOperationException(BuildPoolInsufficientMessage(
                lottery, request.Quantity, pool, "na criação do QR Code"));

        var user = await _nauth.GetCurrentAsync()
            ?? throw new InvalidOperationException("Usuário autenticado não encontrado no NAuth.");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(user.Name)) missing.Add("name");
        if (string.IsNullOrWhiteSpace(user.Email)) missing.Add("email");
        if (string.IsNullOrWhiteSpace(user.DocumentId)) missing.Add("documentId");
        if (string.IsNullOrWhiteSpace(user.Phone)) missing.Add("cellphone");
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Complete seu cadastro no NAuth antes de comprar (faltam: {string.Join(", ", missing)}).");

        var combo = await _comboRepo.FindMatchingComboAsync(lottery.LotteryId, request.Quantity);
        var subtotal = lottery.TicketPrice * request.Quantity;
        decimal discount = 0;
        if (combo is not null && combo.DiscountValue > 0)
            discount = Math.Round(subtotal * (decimal)(combo.DiscountValue / 100f), 2);
        var totalAmount = subtotal - discount;

        var mode = (TicketOrderMode)request.Mode;

        List<long>? pickedLongs = null;
        if (mode == TicketOrderMode.UserPicks)
        {
            if (request.PickedNumbers is null || request.PickedNumbers.Count != request.Quantity)
                throw new InvalidOperationException("Em UserPicks, pickedNumbers deve ter a mesma quantidade do pedido.");
            pickedLongs = ParseAndValidatePickedNumbers(lottery, request.PickedNumbers);
            if (!await _reservationRepo.AreNumbersAvailableAsync(lottery.LotteryId, pickedLongs))
                throw new InvalidOperationException("Um ou mais números já foram reservados ou vendidos.");

            await _reservationRepo.ExpireByUserAndLotteryAsync(currentUserId, lottery.LotteryId);
            var expires = DateTime.UtcNow.AddMinutes(20);
            var reservations = pickedLongs.Select(n => new NumberReservation
            {
                LotteryId = lottery.LotteryId,
                UserId = currentUserId,
                TicketNumber = n,
                ExpiresAt = expires
            });
            await _reservationRepo.InsertBatchAsync(reservations);
        }

        if (string.IsNullOrWhiteSpace(lottery.StoreClientId))
            throw new InvalidOperationException(
                $"Lottery {lottery.LotteryId} sem storeClientId em cache. " +
                "Republique a Lottery (publish) para sincronizar com o ProxyPay.");

        var qr = await _proxyPay.CreateQRCodeAsync(new ProxyPayQRCodeRequest
        {
            ClientId = lottery.StoreClientId,
            Customer = new ProxyPayCustomer
            {
                Name = user.Name,
                Email = user.Email,
                DocumentId = user.DocumentId!,
                Cellphone = user.Phone!
            },
            Items = new List<ProxyPayItem>
            {
                new()
                {
                    Id = $"LOTTERY-{lottery.LotteryId}",
                    Description = $"Fortuno Lottery #{lottery.LotteryId} - {request.Quantity} tickets",
                    Quantity = request.Quantity,
                    UnitPrice = lottery.TicketPrice,
                    Discount = discount
                }
            }
        });

        if (mode == TicketOrderMode.UserPicks)
        {
            var mine = await _reservationRepo.ListByUserAndLotteryAsync(currentUserId, lottery.LotteryId);
            foreach (var r in mine.Where(x => x.InvoiceId is null))
            {
                r.InvoiceId = qr.InvoiceId;
                r.ExpiresAt = qr.ExpiredAt;
                await _reservationRepo.UpdateAsync(r);
            }
        }

        var order = new TicketOrder
        {
            InvoiceId = qr.InvoiceId,
            InvoiceNumber = qr.InvoiceNumber,
            UserId = currentUserId,
            LotteryId = lottery.LotteryId,
            Quantity = request.Quantity,
            Mode = mode,
            ReferralCode = request.ReferralCode,
            ReferralPercentAtPurchase = lottery.ReferralPercent,
            TotalAmount = totalAmount,
            BrCode = qr.BrCode,
            BrCodeBase64 = qr.BrCodeBase64,
            ExpiredAt = qr.ExpiredAt,
            Status = TicketOrderStatus.Pending
        };
        await _orderRepo.InsertAsync(order);

        if (mode == TicketOrderMode.UserPicks && pickedLongs is { Count: > 0 })
        {
            var numberRows = pickedLongs.Select(n => new TicketOrderNumber
            {
                TicketOrderId = order.TicketOrderId,
                TicketNumber = n
            });
            await _orderNumberRepo.InsertBatchAsync(numberRows);
        }

        return new TicketQRCodeInfo
        {
            InvoiceId = qr.InvoiceId,
            InvoiceNumber = qr.InvoiceNumber,
            BrCode = qr.BrCode,
            BrCodeBase64 = qr.BrCodeBase64,
            ExpiredAt = qr.ExpiredAt
        };
    }

    // ---------- POST /tickets/reserve-number ----------

    public async Task<NumberReservationResult> ReserveNumberAsync(long currentUserId, NumberReservationRequest request)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(request.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {request.LotteryId} não encontrada.");

        if (lottery.Status != LotteryStatus.Open)
            throw new InvalidOperationException("Reserva disponível apenas em Lottery Open.");

        // Parse + validação de faixa/composição (string → long canônico).
        if (!_numbers.TryParse(lottery.NumberType, request.TicketNumber, out var numberValue))
            throw new InvalidOperationException(
                $"Número '{request.TicketNumber}' em formato inválido para tipo {lottery.NumberType}.");

        ValidateNumberRange(lottery, numberValue, request.TicketNumber);

        var displayNumber = _numbers.Format(lottery.NumberType, numberValue);

        var sold = await _tickets.GetByLotteryAndNumberAsync(lottery.LotteryId, numberValue);
        if (sold is not null)
        {
            return new NumberReservationResult
            {
                Success = false,
                Status = NumberReservationStatusDto.AlreadyPurchased,
                Message = $"Número {displayNumber} já foi comprado.",
                LotteryId = lottery.LotteryId,
                TicketNumber = displayNumber
            };
        }

        if (await _reservationRepo.IsNumberReservedAsync(lottery.LotteryId, numberValue))
        {
            return new NumberReservationResult
            {
                Success = false,
                Status = NumberReservationStatusDto.AlreadyReserved,
                Message = $"Número {displayNumber} já está reservado.",
                LotteryId = lottery.LotteryId,
                TicketNumber = displayNumber
            };
        }

        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        await _reservationRepo.InsertBatchAsync(new[]
        {
            new NumberReservation
            {
                LotteryId = lottery.LotteryId,
                UserId = currentUserId,
                TicketNumber = numberValue,
                ExpiresAt = expiresAt
            }
        });

        return new NumberReservationResult
        {
            Success = true,
            Status = NumberReservationStatusDto.Reserved,
            Message = $"Número {displayNumber} reservado com sucesso por 5 minutos.",
            LotteryId = lottery.LotteryId,
            TicketNumber = displayNumber,
            ExpiresAt = expiresAt
        };
    }

    // Int64: faixa = [TicketNumIni, TicketNumEnd]. Composed: cada componente em [NumberValueMin, NumberValueMax].
    private void ValidateNumberRange(Lottery lottery, long numberValue, string displayInput)
    {
        if (lottery.NumberType == NumberType.Int64)
        {
            if (numberValue < lottery.TicketNumIni || numberValue > lottery.TicketNumEnd)
                throw new InvalidOperationException(
                    $"Número {displayInput} fora da faixa [{lottery.TicketNumIni}..{lottery.TicketNumEnd}].");
        }
        else
        {
            if (!_numbers.IsValid(lottery.NumberType, numberValue, lottery.NumberValueMin, lottery.NumberValueMax))
                throw new InvalidOperationException(
                    $"Número {displayInput}: componentes fora da faixa [{lottery.NumberValueMin}..{lottery.NumberValueMax}].");
        }
    }

    // ---------- GET /tickets/qrcode/{invoiceId}/status ----------

    public async Task<TicketQRCodeStatusInfo> CheckQRCodeStatusAsync(long invoiceId)
    {
        var order = await _orderRepo.GetByInvoiceIdAsync(invoiceId);
        if (order is null)
            return new TicketQRCodeStatusInfo { Status = null, InvoiceId = invoiceId };

        var info = new TicketQRCodeStatusInfo
        {
            InvoiceId = invoiceId,
            InvoiceNumber = order.InvoiceNumber,
            ExpiredAt = order.ExpiredAt
        };

        // Fast-path: status terminal já persistido → NÃO consulta provedor e NÃO emite tickets.
        // Idempotência garantida por este early-return: tickets só são emitidos na transição
        // Pending → Paid (abaixo, dentro de ProcessPaymentAsync).
        switch (order.Status)
        {
            case TicketOrderStatus.Paid:
                return await BuildPaidResponseAsync(order, info);
            case TicketOrderStatus.Expired:
                info.Status = (int)TicketOrderStatus.Expired;
                return info;
            case TicketOrderStatus.Cancelled:
                info.Status = (int)TicketOrderStatus.Cancelled;
                return info;
        }

        // A partir daqui order.Status == Pending. Consulta o provedor.
        var providerResponse = await _proxyPay.GetQRCodeStatusAsync(invoiceId);
        var providerStatus = MapProviderStatus(providerResponse?.Status);

        switch (providerStatus)
        {
            case TicketOrderStatus.Paid:
                // ÚNICA porta de entrada para emissão de tickets.
                return await ProcessPaymentAsync(order, info);

            case TicketOrderStatus.Expired:
                await _orderRepo.TryMarkExpiredAsync(order.TicketOrderId);
                info.Status = (int)TicketOrderStatus.Expired;
                return info;

            case TicketOrderStatus.Cancelled:
                await _orderRepo.TryMarkCancelledAsync(order.TicketOrderId);
                info.Status = (int)TicketOrderStatus.Cancelled;
                return info;

            case TicketOrderStatus.Pending:
            case TicketOrderStatus.Sent:
                info.Status = (int)providerStatus.Value;
                info.BrCode = order.BrCode;
                info.BrCodeBase64 = order.BrCodeBase64;
                return info;

            default:
                info.Status = null;
                return info;
        }
    }

    // ---------- ProcessPayment (idempotente, R-002) ----------

    private async Task<TicketQRCodeStatusInfo> ProcessPaymentAsync(TicketOrder order, TicketQRCodeStatusInfo info)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(order.LotteryId);
        if (lottery is null || lottery.Status != LotteryStatus.Open)
        {
            // Provedor confirmou pagamento mas Lottery não está Open — marca Paid para
            // preservar idempotência (próximo poll não re-tenta) e lança erro para o caller.
            await _orderRepo.TryMarkPaidAsync(order.TicketOrderId);
            throw new InvalidOperationException(
                "Lottery não estava Open no momento do pagamento — refund manual necessário.");
        }

        List<long> finalNumbers;
        if (order.Mode == TicketOrderMode.UserPicks)
        {
            var reservations = await _reservationRepo.ListByUserAndLotteryAsync(order.UserId, order.LotteryId);
            var linked = reservations.Where(r => r.InvoiceId == order.InvoiceId && r.ExpiresAt > DateTime.UtcNow).ToList();
            if (linked.Count != order.Quantity)
            {
                await _orderRepo.TryMarkPaidAsync(order.TicketOrderId);
                throw new InvalidOperationException(
                    "Reservas expiraram antes do pagamento — refund manual necessário.");
            }
            finalNumbers = linked.Select(r => r.TicketNumber).ToList();
        }
        else
        {
            finalNumbers = await DrawRandomAvailableNumbersAsync(lottery, order.Quantity);
            if (finalNumbers.Count != order.Quantity)
            {
                var poolAtPayment = await ComputePoolStatsAsync(lottery);
                await _orderRepo.TryMarkPaidAsync(order.TicketOrderId);
                throw new InvalidOperationException(BuildPoolInsufficientMessage(
                    lottery, order.Quantity, poolAtPayment, "no momento do pagamento") +
                    " Refund manual necessário.");
            }
        }

        var rows = await _orderRepo.TryMarkPaidAsync(order.TicketOrderId);
        if (rows == 0)
        {
            // concorrência: outra thread ganhou — retorna estado atual
            return await BuildPaidResponseAsync(order, info);
        }

        var tickets = finalNumbers.Select(n => new Ticket
        {
            LotteryId = order.LotteryId,
            UserId = order.UserId,
            InvoiceId = order.InvoiceId,
            TicketNumber = n,
            TicketValue = _numbers.Format(lottery.NumberType, n),
            RefundState = TicketRefundState.None
        }).ToList();
        await _tickets.InsertBatchAsync(tickets);

        if (!string.IsNullOrWhiteSpace(order.ReferralCode))
        {
            var referrerId = await _referrer.ResolveReferrerUserIdAsync(order.ReferralCode);
            if (referrerId.HasValue && referrerId.Value != order.UserId)
            {
                await _invoiceReferrerRepo.InsertAsync(new InvoiceReferrer
                {
                    InvoiceId = order.InvoiceId,
                    ReferrerUserId = referrerId.Value,
                    LotteryId = order.LotteryId,
                    ReferralPercentAtPurchase = order.ReferralPercentAtPurchase
                });
            }
        }

        info.Status = (int)TicketOrderStatus.Paid;
        info.Tickets = await LoadTicketsForInvoiceAsync(order.InvoiceId);
        return info;
    }

    private async Task<TicketQRCodeStatusInfo> BuildPaidResponseAsync(TicketOrder order, TicketQRCodeStatusInfo info)
    {
        var existing = await LoadTicketsForInvoiceAsync(order.InvoiceId);
        if (existing.Count == 0)
        {
            // Estado anômalo: order marcada Paid mas sem tickets no DB.
            // Toda consulta subsequente reporta este mesmo erro (idempotente).
            throw new InvalidOperationException(
                "Pagamento confirmado mas nenhum ticket foi emitido — refund manual necessário.");
        }

        info.Status = (int)TicketOrderStatus.Paid;
        info.Tickets = existing;
        return info;
    }

    private async Task<List<TicketInfo>> LoadTicketsForInvoiceAsync(long invoiceId)
    {
        var list = await _tickets.ListByInvoiceAsync(invoiceId);
        return list.Select(MapToDto).ToList();
    }

    // ---------- helpers ----------

    // TicketOrderStatus está pareado 1:1 com ProxyPay.InvoiceStatusEnum.
    // Ints reconhecidos (1..6) caem no cast direto; nulo/desconhecido vira null.
    private static TicketOrderStatus? MapProviderStatus(int? raw) =>
        raw is >= 1 and <= 6
            ? (TicketOrderStatus)raw.Value
            : null;

    // Int64: pool = [TicketNumIni, TicketNumEnd]. Composed: pool = combinatório [NumberValueMin, NumberValueMax] nos N componentes.
    private async Task<PoolStats> ComputePoolStatsAsync(Lottery lottery)
    {
        long poolTotal;
        if (lottery.NumberType == NumberType.Int64)
            poolTotal = Math.Max(0L, lottery.TicketNumEnd - lottery.TicketNumIni + 1);
        else
            poolTotal = _numbers.CountPossibilities(
                lottery.NumberType, lottery.NumberValueMin, lottery.NumberValueMax);

        var sold = await _tickets.CountSoldAsync(lottery.LotteryId);
        var reserved = (await _reservationRepo.ListActiveReservedNumbersAsync(lottery.LotteryId)).Count;
        var available = Math.Max(0L, poolTotal - sold - reserved);
        return new PoolStats(poolTotal, sold, reserved, available);
    }

    private static string BuildPoolInsufficientMessage(
        Lottery lottery, int requested, PoolStats pool, string context)
    {
        var faixa = lottery.NumberType == NumberType.Int64
            ? $"faixa {lottery.TicketNumIni}–{lottery.TicketNumEnd}"
            : $"componentes {lottery.NumberValueMin}–{lottery.NumberValueMax} em {lottery.NumberType}";
        return
            $"Pool de números insuficiente {context}: " +
            $"restam {pool.Available} disponível(is) para compra " +
            $"(total {pool.PoolTotal} — {faixa}; {pool.Sold} já vendido(s), {pool.Reserved} reservado(s)) " +
            $"e {requested} foram solicitados.";
    }

    private readonly record struct PoolStats(long PoolTotal, long Sold, int Reserved, long Available);

    // Parse string-format pickedNumbers, valida faixa conforme tipo e retorna os
    // long canônicos (já ordenados para composed via NumberCompositionService.Parse).
    private List<long> ParseAndValidatePickedNumbers(Lottery lottery, IReadOnlyList<string> inputs)
    {
        var parsed = new List<long>(inputs.Count);
        foreach (var raw in inputs)
        {
            if (!_numbers.TryParse(lottery.NumberType, raw, out var value))
                throw new InvalidOperationException(
                    $"Número '{raw}' em formato inválido para tipo {lottery.NumberType}.");
            ValidateNumberRange(lottery, value, raw);
            parsed.Add(value);
        }

        if (parsed.Distinct().Count() != parsed.Count)
            throw new InvalidOperationException("Números escolhidos duplicados.");

        return parsed;
    }

    private async Task<List<long>> DrawRandomAvailableNumbersAsync(Lottery lottery, int quantity)
    {
        var sold = new HashSet<long>(await _tickets.ListSoldNumbersAsync(lottery.LotteryId));
        var reserved = new HashSet<long>(await _reservationRepo.ListActiveReservedNumbersAsync(lottery.LotteryId));

        var rng = Random.Shared;
        var picks = new List<long>();

        if (lottery.NumberType == NumberType.Int64)
        {
            // Int64: faixa = [TicketNumIni, TicketNumEnd].
            var min = lottery.TicketNumIni;
            var max = lottery.TicketNumEnd;
            var poolSize = max - min + 1;
            if (poolSize <= 0) return picks;

            if (poolSize <= 1_000_000)
            {
                var pool = new List<long>((int)poolSize);
                for (long v = min; v <= max; v++)
                    if (!sold.Contains(v) && !reserved.Contains(v)) pool.Add(v);
                Shuffle(pool, rng);
                picks = pool.Take(quantity).ToList();
            }
            else
            {
                int attempts = 0;
                while (picks.Count < quantity && attempts < quantity * 20)
                {
                    var v = rng.NextInt64(min, max + 1);
                    if (sold.Contains(v) || reserved.Contains(v) || picks.Contains(v)) { attempts++; continue; }
                    picks.Add(v);
                }
            }
        }
        else
        {
            // Composed: pool = combinatório de N componentes em [NumberValueMin, NumberValueMax].
            var poolSize = _numbers.CountPossibilities(
                lottery.NumberType, lottery.NumberValueMin, lottery.NumberValueMax);

            if (poolSize <= 0) return picks;

            if (poolSize <= 1_000_000)
            {
                var pool = new List<long>();
                foreach (var v in _numbers.EnumerateAll(lottery.NumberType, lottery.NumberValueMin, lottery.NumberValueMax))
                {
                    if (sold.Contains(v) || reserved.Contains(v)) continue;
                    pool.Add(v);
                }
                Shuffle(pool, rng);
                picks = pool.Take(quantity).ToList();
            }
            else
            {
                var n = _numbers.ComponentCount(lottery.NumberType);
                var cmin = lottery.NumberValueMin;
                var cmax = lottery.NumberValueMax;
                var picksSet = new HashSet<long>();
                int attempts = 0;
                while (picks.Count < quantity && attempts < quantity * 20)
                {
                    var components = new int[n];
                    for (int i = 0; i < n; i++) components[i] = rng.Next(cmin, cmax + 1);
                    var v = _numbers.Compose(lottery.NumberType, components);
                    if (sold.Contains(v) || reserved.Contains(v) || !picksSet.Add(v)) { attempts++; continue; }
                    picks.Add(v);
                }
            }
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

    private TicketInfo MapToDto(Ticket t)
    {
        // Fallback: para tickets legados sem ticket_value persistido, computa on-the-fly.
        var value = !string.IsNullOrEmpty(t.TicketValue)
            ? t.TicketValue
            : _numbers.Format(t.Lottery?.NumberType ?? NumberType.Int64, t.TicketNumber);

        return new TicketInfo
        {
            TicketId = t.TicketId,
            LotteryId = t.LotteryId,
            UserId = t.UserId,
            InvoiceId = t.InvoiceId,
            TicketNumber = t.TicketNumber,
            TicketValue = value,
            RefundState = (TicketRefundStateDto)t.RefundState,
            CreatedAt = t.CreatedAt
        };
    }
}
