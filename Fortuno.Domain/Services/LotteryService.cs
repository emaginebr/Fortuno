using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class LotteryService : ILotteryService
{
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly ILotteryImageRepository<LotteryImage> _imageRepo;
    private readonly IRaffleRepository<Raffle> _raffleRepo;
    private readonly IRaffleAwardRepository<RaffleAward> _awardRepo;
    private readonly ITicketRepository<Ticket> _ticketRepo;
    private readonly ISlugService _slugService;
    private readonly IStoreOwnershipGuard _ownership;
    private readonly INumberCompositionService _numbers;

    public LotteryService(
        ILotteryRepository<Lottery> lotteryRepo,
        ILotteryImageRepository<LotteryImage> imageRepo,
        IRaffleRepository<Raffle> raffleRepo,
        IRaffleAwardRepository<RaffleAward> awardRepo,
        ITicketRepository<Ticket> ticketRepo,
        ISlugService slugService,
        IStoreOwnershipGuard ownership,
        INumberCompositionService numbers)
    {
        _lotteryRepo = lotteryRepo;
        _imageRepo = imageRepo;
        _raffleRepo = raffleRepo;
        _awardRepo = awardRepo;
        _ticketRepo = ticketRepo;
        _slugService = slugService;
        _ownership = ownership;
        _numbers = numbers;
    }

    public async Task<LotteryInfo> CreateAsync(long currentUserId, LotteryInsertInfo dto)
    {
        await _ownership.EnsureOwnershipAsync(dto.StoreId, currentUserId);

        var slug = await _slugService.GenerateUniqueSlugAsync(dto.Name);
        var entity = new Lottery
        {
            StoreId = dto.StoreId,
            Name = dto.Name,
            Slug = slug,
            DescriptionMd = dto.DescriptionMd,
            RulesMd = dto.RulesMd,
            PrivacyPolicyMd = dto.PrivacyPolicyMd,
            TicketPrice = dto.TicketPrice,
            TotalPrizeValue = dto.TotalPrizeValue,
            TicketMin = dto.TicketMin,
            TicketMax = dto.TicketMax,
            TicketNumIni = dto.TicketNumIni,
            TicketNumEnd = dto.TicketNumEnd,
            NumberType = (NumberType)dto.NumberType,
            NumberValueMin = dto.NumberValueMin,
            NumberValueMax = dto.NumberValueMax,
            ReferralPercent = dto.ReferralPercent,
            Status = LotteryStatus.Draft
        };
        var saved = await _lotteryRepo.InsertAsync(entity);
        return MapToDto(saved);
    }

    public async Task<LotteryInfo> UpdateAsync(long currentUserId, long lotteryId, LotteryUpdateInfo dto)
    {
        var entity = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(entity.StoreId, currentUserId);

        if (entity.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Edição permitida apenas em Lottery Draft.");

        if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != entity.Slug)
        {
            var newSlug = dto.Slug.ToLowerInvariant();
            if (await _lotteryRepo.SlugExistsAsync(newSlug))
                throw new InvalidOperationException($"Slug '{newSlug}' já em uso.");
            entity.Slug = newSlug;
        }

        entity.Name = dto.Name;
        entity.DescriptionMd = dto.DescriptionMd;
        entity.RulesMd = dto.RulesMd;
        entity.PrivacyPolicyMd = dto.PrivacyPolicyMd;
        entity.TicketPrice = dto.TicketPrice;
        entity.TotalPrizeValue = dto.TotalPrizeValue;
        entity.TicketMin = dto.TicketMin;
        entity.TicketMax = dto.TicketMax;
        entity.TicketNumIni = dto.TicketNumIni;
        entity.TicketNumEnd = dto.TicketNumEnd;
        entity.NumberType = (NumberType)dto.NumberType;
        entity.NumberValueMin = dto.NumberValueMin;
        entity.NumberValueMax = dto.NumberValueMax;
        entity.ReferralPercent = dto.ReferralPercent;
        entity.UpdatedAt = DateTime.UtcNow;

        var saved = await _lotteryRepo.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task<LotteryInfo?> GetByIdAsync(long lotteryId)
    {
        var entity = await _lotteryRepo.GetByIdWithDetailsAsync(lotteryId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<LotteryInfo?> GetBySlugAsync(string slug)
    {
        var entity = await _lotteryRepo.GetBySlugAsync(slug);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<List<LotteryInfo>> ListByStoreAsync(long storeId)
    {
        var list = await _lotteryRepo.ListByStoreAsync(storeId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<LotteryInfo>> ListOpenAsync()
    {
        var list = await _lotteryRepo.ListOpenAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<LotteryInfo> PublishAsync(long currentUserId, long lotteryId)
    {
        var entity = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(entity.StoreId, currentUserId);

        if (entity.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Somente Lottery em Draft pode ser publicada.");

        var errors = new List<string>();
        if (await _imageRepo.CountByLotteryAsync(lotteryId) < 1) errors.Add("Ao menos 1 imagem é obrigatória.");
        if (await _raffleRepo.CountByLotteryAsync(lotteryId) < 1) errors.Add("Ao menos 1 Raffle é obrigatório.");

        var raffles = await _raffleRepo.ListByLotteryAsync(lotteryId);
        var totalAwards = 0;
        foreach (var r in raffles) totalAwards += await _awardRepo.CountByRaffleAsync(r.RaffleId);
        if (totalAwards < 1) errors.Add("Ao menos 1 RaffleAward é obrigatório.");

        if (entity.NumberType != NumberType.Int64 && entity.TicketNumEnd <= entity.TicketNumIni)
            errors.Add("Para tipos compostos, TicketNumEnd deve ser maior que TicketNumIni.");

        if (errors.Count > 0)
            throw new InvalidOperationException("Requisitos de publicação não atendidos: " + string.Join(" | ", errors));

        entity.Status = LotteryStatus.Open;
        entity.UpdatedAt = DateTime.UtcNow;
        var saved = await _lotteryRepo.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task<LotteryInfo> CloseAsync(long currentUserId, long lotteryId)
    {
        var entity = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(entity.StoreId, currentUserId);

        if (entity.Status != LotteryStatus.Open)
            throw new InvalidOperationException("Somente Lottery em Open pode ser fechada.");

        entity.Status = LotteryStatus.Closed;
        entity.UpdatedAt = DateTime.UtcNow;
        var saved = await _lotteryRepo.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task<LotteryInfo> CancelAsync(long currentUserId, long lotteryId, LotteryCancelRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 20)
            throw new InvalidOperationException("Motivo do cancelamento é obrigatório (mínimo 20 caracteres).");

        var entity = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(entity.StoreId, currentUserId);

        if (entity.Status == LotteryStatus.Cancelled)
            throw new InvalidOperationException("Lottery já está cancelada.");

        // Marcar tickets ativos como PendingRefund (FR-033a)
        var tickets = await _ticketRepo.ListByLotteryAsync(lotteryId);
        var activeIds = tickets.Where(t => t.RefundState == TicketRefundState.None).Select(t => t.TicketId).ToList();
        if (activeIds.Count > 0)
            await _ticketRepo.MarkRefundStateAsync(activeIds, (int)TicketRefundState.PendingRefund);

        entity.Status = LotteryStatus.Cancelled;
        entity.CancelReason = request.Reason.Trim();
        entity.CancelledByUserId = currentUserId;
        entity.CancelledAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        var saved = await _lotteryRepo.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task<long> CalculatePossibilitiesAsync(long lotteryId)
    {
        var entity = await _lotteryRepo.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");
        return _numbers.CountPossibilities(entity.NumberType, entity.NumberValueMin, entity.NumberValueMax);
    }

    private static LotteryInfo MapToDto(Lottery e) => new()
    {
        LotteryId = e.LotteryId,
        StoreId = e.StoreId,
        Name = e.Name,
        Slug = e.Slug,
        DescriptionMd = e.DescriptionMd,
        RulesMd = e.RulesMd,
        PrivacyPolicyMd = e.PrivacyPolicyMd,
        TicketPrice = e.TicketPrice,
        TotalPrizeValue = e.TotalPrizeValue,
        TicketMin = e.TicketMin,
        TicketMax = e.TicketMax,
        TicketNumIni = e.TicketNumIni,
        TicketNumEnd = e.TicketNumEnd,
        NumberType = (NumberTypeDto)e.NumberType,
        NumberValueMin = e.NumberValueMin,
        NumberValueMax = e.NumberValueMax,
        ReferralPercent = e.ReferralPercent,
        Status = (LotteryStatusDto)e.Status,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Images = e.Images?.Select(MapImage).ToList() ?? new(),
        Combos = e.Combos?.Select(MapCombo).ToList() ?? new(),
        Raffles = e.Raffles?.Select(MapRaffle).ToList() ?? new()
    };

    private static Fortuno.DTO.LotteryImage.LotteryImageInfo MapImage(LotteryImage i) => new()
    {
        LotteryImageId = i.LotteryImageId,
        LotteryId = i.LotteryId,
        ImageUrl = i.ImageUrl,
        Description = i.Description,
        DisplayOrder = i.DisplayOrder,
        CreatedAt = i.CreatedAt
    };

    private static Fortuno.DTO.LotteryCombo.LotteryComboInfo MapCombo(LotteryCombo c) => new()
    {
        LotteryComboId = c.LotteryComboId,
        LotteryId = c.LotteryId,
        Name = c.Name,
        DiscountValue = c.DiscountValue,
        DiscountLabel = c.DiscountLabel,
        QuantityStart = c.QuantityStart,
        QuantityEnd = c.QuantityEnd,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static Fortuno.DTO.Raffle.RaffleInfo MapRaffle(Raffle r) => new()
    {
        RaffleId = r.RaffleId,
        LotteryId = r.LotteryId,
        Name = r.Name,
        DescriptionMd = r.DescriptionMd,
        RaffleDatetime = r.RaffleDatetime,
        VideoUrl = r.VideoUrl,
        IncludePreviousWinners = r.IncludePreviousWinners,
        Status = (RaffleStatusDto)r.Status,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt,
        Awards = r.Awards?.Select(MapAward).ToList() ?? new()
    };

    private static Fortuno.DTO.RaffleAward.RaffleAwardInfo MapAward(RaffleAward a) => new()
    {
        RaffleAwardId = a.RaffleAwardId,
        RaffleId = a.RaffleId,
        Position = a.Position,
        Description = a.Description
    };
}
