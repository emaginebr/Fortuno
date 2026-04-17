using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.LotteryImage;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class LotteryImageService : ILotteryImageService
{
    private readonly ILotteryImageRepository<LotteryImage> _imageRepo;
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly IStoreOwnershipGuard _ownership;
    private readonly IZToolsAppService _zTools;

    public LotteryImageService(
        ILotteryImageRepository<LotteryImage> imageRepo,
        ILotteryRepository<Lottery> lotteryRepo,
        IStoreOwnershipGuard ownership,
        IZToolsAppService zTools)
    {
        _imageRepo = imageRepo;
        _lotteryRepo = lotteryRepo;
        _ownership = ownership;
        _zTools = zTools;
    }

    public async Task<LotteryImageInfo> CreateAsync(long currentUserId, LotteryImageInsertInfo dto)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(dto.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {dto.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Upload permitido apenas em Lottery Draft.");

        var fileName = $"lottery-{lottery.LotteryId}-{Guid.NewGuid():N}.png";
        var url = await _zTools.UploadImageAsync(dto.ImageBase64, fileName);

        var entity = new LotteryImage
        {
            LotteryId = dto.LotteryId,
            ImageUrl = url,
            Description = dto.Description,
            DisplayOrder = dto.DisplayOrder
        };
        var saved = await _imageRepo.InsertAsync(entity);
        return MapToDto(saved);
    }

    public async Task<LotteryImageInfo> UpdateAsync(long currentUserId, long imageId, LotteryImageUpdateInfo dto)
    {
        var entity = await _imageRepo.GetByIdAsync(imageId)
            ?? throw new KeyNotFoundException($"LotteryImage {imageId} não encontrada.");
        var lottery = await _lotteryRepo.GetByIdAsync(entity.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {entity.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Edição permitida apenas em Lottery Draft.");

        entity.Description = dto.Description;
        entity.DisplayOrder = dto.DisplayOrder;
        var saved = await _imageRepo.UpdateAsync(entity);
        return MapToDto(saved);
    }

    public async Task DeleteAsync(long currentUserId, long imageId)
    {
        var entity = await _imageRepo.GetByIdAsync(imageId)
            ?? throw new KeyNotFoundException($"LotteryImage {imageId} não encontrada.");
        var lottery = await _lotteryRepo.GetByIdAsync(entity.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {entity.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Exclusão permitida apenas em Lottery Draft.");

        await _imageRepo.DeleteAsync(imageId);
    }

    public async Task<List<LotteryImageInfo>> ListByLotteryAsync(long lotteryId)
    {
        var list = await _imageRepo.ListByLotteryAsync(lotteryId);
        return list.Select(MapToDto).ToList();
    }

    private static LotteryImageInfo MapToDto(LotteryImage e) => new()
    {
        LotteryImageId = e.LotteryImageId,
        LotteryId = e.LotteryId,
        ImageUrl = e.ImageUrl,
        Description = e.Description,
        DisplayOrder = e.DisplayOrder,
        CreatedAt = e.CreatedAt
    };
}
