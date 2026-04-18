using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.LotteryImage;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class LotteryImageServiceTests
{
    private readonly Mock<ILotteryImageRepository<LotteryImage>> _imageRepo = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();
    private readonly Mock<IZToolsAppService> _zTools = new();

    private LotteryImageService CreateSut() =>
        new(_imageRepo.Object, _lotteryRepo.Object, _ownership.Object, _zTools.Object);

    private void SetupLottery(LotteryStatus status = LotteryStatus.Draft)
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = status });
    }

    [Fact]
    public async Task CreateAsync_ShouldUploadAndPersist()
    {
        SetupLottery();
        _zTools.Setup(z => z.UploadImageAsync("BASE64", It.IsAny<string>()))
            .ReturnsAsync("https://s3/xyz.png");
        _imageRepo.Setup(i => i.InsertAsync(It.IsAny<LotteryImage>()))
            .ReturnsAsync((LotteryImage x) => { x.LotteryImageId = 7; return x; });

        var sut = CreateSut();
        var info = await sut.CreateAsync(42, new LotteryImageInsertInfo
        {
            LotteryId = 1,
            ImageBase64 = "BASE64",
            Description = "Banner",
            DisplayOrder = 0
        });

        info.LotteryImageId.Should().Be(7);
        info.ImageUrl.Should().Be("https://s3/xyz.png");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        SetupLottery(LotteryStatus.Open);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new LotteryImageInsertInfo
        {
            LotteryId = 1,
            ImageBase64 = "BASE64"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenLotteryMissing()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new LotteryImageInsertInfo { LotteryId = 1 });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldApplyChanges()
    {
        _imageRepo.Setup(i => i.GetByIdAsync(7))
            .ReturnsAsync(new LotteryImage { LotteryImageId = 7, LotteryId = 1 });
        SetupLottery();
        _imageRepo.Setup(i => i.UpdateAsync(It.IsAny<LotteryImage>()))
            .ReturnsAsync((LotteryImage x) => x);

        var sut = CreateSut();
        var info = await sut.UpdateAsync(42, 7, new LotteryImageUpdateInfo
        {
            Description = "novo",
            DisplayOrder = 2
        });

        info.Description.Should().Be("novo");
        info.DisplayOrder.Should().Be(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenImageMissing()
    {
        _imageRepo.Setup(i => i.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((LotteryImage?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.UpdateAsync(42, 7, new LotteryImageUpdateInfo());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallRepo_WhenLotteryDraft()
    {
        _imageRepo.Setup(i => i.GetByIdAsync(7))
            .ReturnsAsync(new LotteryImage { LotteryImageId = 7, LotteryId = 1 });
        SetupLottery();

        var sut = CreateSut();
        await sut.DeleteAsync(42, 7);

        _imageRepo.Verify(i => i.DeleteAsync(7), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        _imageRepo.Setup(i => i.GetByIdAsync(7))
            .ReturnsAsync(new LotteryImage { LotteryImageId = 7, LotteryId = 1 });
        SetupLottery(LotteryStatus.Cancelled);

        var sut = CreateSut();
        Func<Task> act = () => sut.DeleteAsync(42, 7);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListByLotteryAsync_ShouldReturnAll()
    {
        _imageRepo.Setup(i => i.ListByLotteryAsync(1)).ReturnsAsync(new List<LotteryImage>
        {
            new() { LotteryImageId = 1 }, new() { LotteryImageId = 2 }
        });

        var sut = CreateSut();
        (await sut.ListByLotteryAsync(1)).Should().HaveCount(2);
    }
}
