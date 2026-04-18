using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.LotteryCombo;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class LotteryComboServiceTests
{
    private readonly Mock<ILotteryComboRepository<LotteryCombo>> _combos = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteries = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();

    private LotteryComboService CreateSut() =>
        new(_combos.Object, _lotteries.Object, _ownership.Object);

    private void SetupLottery(LotteryStatus status = LotteryStatus.Draft)
    {
        _lotteries.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = status });
    }

    [Fact]
    public async Task CreateAsync_ShouldInsert_WhenNoOverlap()
    {
        SetupLottery();
        _combos.Setup(c => c.ListByLotteryAsync(1)).ReturnsAsync(new List<LotteryCombo>());
        _combos.Setup(c => c.InsertAsync(It.IsAny<LotteryCombo>()))
            .ReturnsAsync((LotteryCombo c) => { c.LotteryComboId = 9; return c; });

        var sut = CreateSut();
        var info = await sut.CreateAsync(42, new LotteryComboInsertInfo
        {
            LotteryId = 1,
            Name = "5-pack",
            DiscountValue = 10f,
            DiscountLabel = "10% off",
            QuantityStart = 5,
            QuantityEnd = 10
        });

        info.LotteryComboId.Should().Be(9);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenRangeOverlaps()
    {
        SetupLottery();
        _combos.Setup(c => c.ListByLotteryAsync(1)).ReturnsAsync(new List<LotteryCombo>
        {
            new() { LotteryComboId = 1, Name = "existente", QuantityStart = 3, QuantityEnd = 10 }
        });

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new LotteryComboInsertInfo
        {
            LotteryId = 1,
            Name = "novo",
            DiscountValue = 10f,
            DiscountLabel = "10%",
            QuantityStart = 5,
            QuantityEnd = 12
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sobreposta*");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        SetupLottery(LotteryStatus.Open);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new LotteryComboInsertInfo { LotteryId = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenEndLessThanStart()
    {
        SetupLottery();
        _combos.Setup(c => c.ListByLotteryAsync(1)).ReturnsAsync(new List<LotteryCombo>());

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new LotteryComboInsertInfo
        {
            LotteryId = 1,
            Name = "inv",
            QuantityStart = 10,
            QuantityEnd = 2
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldApplyChanges()
    {
        _combos.Setup(c => c.GetByIdAsync(9))
            .ReturnsAsync(new LotteryCombo { LotteryComboId = 9, LotteryId = 1, QuantityStart = 1, QuantityEnd = 5 });
        SetupLottery();
        _combos.Setup(c => c.ListByLotteryAsync(1)).ReturnsAsync(new List<LotteryCombo>());
        _combos.Setup(c => c.UpdateAsync(It.IsAny<LotteryCombo>()))
            .ReturnsAsync((LotteryCombo c) => c);

        var sut = CreateSut();
        var info = await sut.UpdateAsync(42, 9, new LotteryComboUpdateInfo
        {
            Name = "novo",
            DiscountValue = 20f,
            DiscountLabel = "20% off",
            QuantityStart = 2,
            QuantityEnd = 10
        });

        info.Name.Should().Be("novo");
        info.QuantityStart.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenComboMissing()
    {
        _combos.Setup(c => c.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((LotteryCombo?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.DeleteAsync(42, 9);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvokeRepo_WhenLotteryDraft()
    {
        _combos.Setup(c => c.GetByIdAsync(9))
            .ReturnsAsync(new LotteryCombo { LotteryComboId = 9, LotteryId = 1 });
        SetupLottery();

        var sut = CreateSut();
        await sut.DeleteAsync(42, 9);

        _combos.Verify(c => c.DeleteAsync(9), Times.Once);
    }

    [Fact]
    public async Task ListByLotteryAsync_ShouldReturnAll()
    {
        _combos.Setup(c => c.ListByLotteryAsync(1)).ReturnsAsync(new List<LotteryCombo>
        {
            new() { LotteryComboId = 1 }, new() { LotteryComboId = 2 }
        });

        var sut = CreateSut();
        (await sut.ListByLotteryAsync(1)).Should().HaveCount(2);
    }
}
