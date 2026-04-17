using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.LotteryCombo;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class LotteryComboService : ILotteryComboService
{
    private readonly ILotteryComboRepository<LotteryCombo> _combos;
    private readonly ILotteryRepository<Lottery> _lotteries;
    private readonly IStoreOwnershipGuard _ownership;

    public LotteryComboService(
        ILotteryComboRepository<LotteryCombo> combos,
        ILotteryRepository<Lottery> lotteries,
        IStoreOwnershipGuard ownership)
    {
        _combos = combos;
        _lotteries = lotteries;
        _ownership = ownership;
    }

    public async Task<LotteryComboInfo> CreateAsync(long currentUserId, LotteryComboInsertInfo dto)
    {
        var lottery = await RequireDraftLotteryAsync(currentUserId, dto.LotteryId);

        await EnsureNoOverlapAsync(dto.LotteryId, dto.QuantityStart, dto.QuantityEnd, ignoreComboId: null);

        var entity = new LotteryCombo
        {
            LotteryId = dto.LotteryId,
            Name = dto.Name,
            DiscountValue = dto.DiscountValue,
            DiscountLabel = dto.DiscountLabel,
            QuantityStart = dto.QuantityStart,
            QuantityEnd = dto.QuantityEnd
        };
        var saved = await _combos.InsertAsync(entity);
        return MapToDto(saved);
    }

    public async Task<LotteryComboInfo> UpdateAsync(long currentUserId, long comboId, LotteryComboUpdateInfo dto)
    {
        var entity = await _combos.GetByIdAsync(comboId)
            ?? throw new KeyNotFoundException($"LotteryCombo {comboId} não encontrado.");
        var lottery = await RequireDraftLotteryAsync(currentUserId, entity.LotteryId);

        await EnsureNoOverlapAsync(entity.LotteryId, dto.QuantityStart, dto.QuantityEnd, ignoreComboId: comboId);

        entity.Name = dto.Name;
        entity.DiscountValue = dto.DiscountValue;
        entity.DiscountLabel = dto.DiscountLabel;
        entity.QuantityStart = dto.QuantityStart;
        entity.QuantityEnd = dto.QuantityEnd;
        entity.UpdatedAt = DateTime.UtcNow;
        var saved = await _combos.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task DeleteAsync(long currentUserId, long comboId)
    {
        var entity = await _combos.GetByIdAsync(comboId)
            ?? throw new KeyNotFoundException($"LotteryCombo {comboId} não encontrado.");
        await RequireDraftLotteryAsync(currentUserId, entity.LotteryId);
        await _combos.DeleteAsync(comboId);
    }

    public async Task<List<LotteryComboInfo>> ListByLotteryAsync(long lotteryId)
    {
        var list = await _combos.ListByLotteryAsync(lotteryId);
        return list.Select(MapToDto).ToList();
    }

    private async Task<Lottery> RequireDraftLotteryAsync(long currentUserId, long lotteryId)
    {
        var lottery = await _lotteries.GetByIdAsync(lotteryId)
            ?? throw new KeyNotFoundException($"Lottery {lotteryId} não encontrada.");
        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);
        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Operação permitida apenas em Lottery Draft.");
        return lottery;
    }

    private async Task EnsureNoOverlapAsync(long lotteryId, int start, int end, long? ignoreComboId)
    {
        if (end > 0 && end < start)
            throw new InvalidOperationException("QuantityEnd deve ser maior ou igual a QuantityStart.");

        var existing = await _combos.ListByLotteryAsync(lotteryId);
        foreach (var c in existing)
        {
            if (ignoreComboId.HasValue && c.LotteryComboId == ignoreComboId.Value) continue;
            if (c.QuantityStart == 0 && c.QuantityEnd == 0) continue; // combos inválidos — ignorados
            if (start <= c.QuantityEnd && end >= c.QuantityStart)
                throw new InvalidOperationException($"Faixa sobreposta com combo '{c.Name}' ({c.QuantityStart}-{c.QuantityEnd}).");
        }
    }

    private static LotteryComboInfo MapToDto(LotteryCombo e) => new()
    {
        LotteryComboId = e.LotteryComboId,
        LotteryId = e.LotteryId,
        Name = e.Name,
        DiscountValue = e.DiscountValue,
        DiscountLabel = e.DiscountLabel,
        QuantityStart = e.QuantityStart,
        QuantityEnd = e.QuantityEnd,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
