using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.RaffleAward;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class RaffleAwardService : IRaffleAwardService
{
    private readonly IRaffleAwardRepository<RaffleAward> _awardRepo;
    private readonly IRaffleRepository<Raffle> _raffleRepo;
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly IStoreOwnershipGuard _ownership;

    public RaffleAwardService(
        IRaffleAwardRepository<RaffleAward> awardRepo,
        IRaffleRepository<Raffle> raffleRepo,
        ILotteryRepository<Lottery> lotteryRepo,
        IStoreOwnershipGuard ownership)
    {
        _awardRepo = awardRepo;
        _raffleRepo = raffleRepo;
        _lotteryRepo = lotteryRepo;
        _ownership = ownership;
    }

    public async Task<RaffleAwardInfo> CreateAsync(long currentUserId, RaffleAwardInsertInfo dto)
    {
        var raffle = await _raffleRepo.GetByIdAsync(dto.RaffleId)
            ?? throw new KeyNotFoundException($"Raffle {dto.RaffleId} não encontrado.");
        var lottery = await _lotteryRepo.GetByIdAsync(raffle.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {raffle.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Criação permitida apenas em Lottery Draft.");

        var entity = new RaffleAward
        {
            RaffleId = dto.RaffleId,
            Position = dto.Position,
            Description = dto.Description
        };
        var saved = await _awardRepo.InsertAsync(entity);
        return MapToDto(saved);
    }

    public async Task<RaffleAwardInfo> UpdateAsync(long currentUserId, long awardId, RaffleAwardUpdateInfo dto)
    {
        var entity = await _awardRepo.GetByIdAsync(awardId)
            ?? throw new KeyNotFoundException($"RaffleAward {awardId} não encontrado.");
        var raffle = await _raffleRepo.GetByIdAsync(entity.RaffleId)
            ?? throw new KeyNotFoundException($"Raffle {entity.RaffleId} não encontrado.");
        var lottery = await _lotteryRepo.GetByIdAsync(raffle.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {raffle.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);
        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Edição permitida apenas em Lottery Draft.");

        entity.Position = dto.Position;
        entity.Description = dto.Description;
        var saved = await _awardRepo.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task DeleteAsync(long currentUserId, long awardId)
    {
        var entity = await _awardRepo.GetByIdAsync(awardId)
            ?? throw new KeyNotFoundException($"RaffleAward {awardId} não encontrado.");
        var raffle = await _raffleRepo.GetByIdAsync(entity.RaffleId)
            ?? throw new KeyNotFoundException($"Raffle {entity.RaffleId} não encontrado.");
        var lottery = await _lotteryRepo.GetByIdAsync(raffle.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {raffle.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);
        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Exclusão permitida apenas em Lottery Draft.");

        await _awardRepo.DeleteAsync(awardId);
    }

    public async Task<List<RaffleAwardInfo>> ListByRaffleAsync(long raffleId)
    {
        var list = await _awardRepo.ListByRaffleAsync(raffleId);
        return list.Select(MapToDto).ToList();
    }

    private static RaffleAwardInfo MapToDto(RaffleAward e) => new()
    {
        RaffleAwardId = e.RaffleAwardId,
        RaffleId = e.RaffleId,
        Position = e.Position,
        Description = e.Description
    };
}
