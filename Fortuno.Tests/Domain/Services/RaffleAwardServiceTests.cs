using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.RaffleAward;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class RaffleAwardServiceTests
{
    private readonly Mock<IRaffleAwardRepository<RaffleAward>> _awardRepo = new();
    private readonly Mock<IRaffleRepository<Raffle>> _raffleRepo = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();

    private RaffleAwardService CreateSut() =>
        new(_awardRepo.Object, _raffleRepo.Object, _lotteryRepo.Object, _ownership.Object);

    private void SetupContext(LotteryStatus lotteryStatus)
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(55))
            .ReturnsAsync(new Raffle { RaffleId = 55, LotteryId = 1 });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = lotteryStatus });
    }

    [Fact]
    public async Task CreateAsync_ShouldInsert_WhenLotteryIsDraft()
    {
        SetupContext(LotteryStatus.Draft);
        _awardRepo.Setup(a => a.InsertAsync(It.IsAny<RaffleAward>()))
            .ReturnsAsync((RaffleAward a) => { a.RaffleAwardId = 123; return a; });

        var sut = CreateSut();
        var info = await sut.CreateAsync(42, new RaffleAwardInsertInfo
        {
            RaffleId = 55,
            Position = 1,
            Description = "1º prêmio"
        });

        info.RaffleAwardId.Should().Be(123);
        info.Position.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        SetupContext(LotteryStatus.Open);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new RaffleAwardInsertInfo
        {
            RaffleId = 55,
            Position = 1,
            Description = "1º"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenRaffleMissing()
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Raffle?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new RaffleAwardInsertInfo { RaffleId = 55 });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersist_WhenLotteryDraft()
    {
        _awardRepo.Setup(a => a.GetByIdAsync(1))
            .ReturnsAsync(new RaffleAward { RaffleAwardId = 1, RaffleId = 55, Position = 1 });
        SetupContext(LotteryStatus.Draft);
        _awardRepo.Setup(a => a.UpdateAsync(It.IsAny<RaffleAward>()))
            .ReturnsAsync((RaffleAward a) => a);

        var sut = CreateSut();
        var info = await sut.UpdateAsync(42, 1, new RaffleAwardUpdateInfo { Position = 2, Description = "novo" });

        info.Position.Should().Be(2);
        info.Description.Should().Be("novo");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenAwardMissing()
    {
        _awardRepo.Setup(a => a.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((RaffleAward?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.UpdateAsync(42, 1, new RaffleAwardUpdateInfo());

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvokeRepo_WhenLotteryDraft()
    {
        _awardRepo.Setup(a => a.GetByIdAsync(1))
            .ReturnsAsync(new RaffleAward { RaffleAwardId = 1, RaffleId = 55 });
        SetupContext(LotteryStatus.Draft);

        var sut = CreateSut();
        await sut.DeleteAsync(42, 1);

        _awardRepo.Verify(a => a.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        _awardRepo.Setup(a => a.GetByIdAsync(1))
            .ReturnsAsync(new RaffleAward { RaffleAwardId = 1, RaffleId = 55 });
        SetupContext(LotteryStatus.Closed);

        var sut = CreateSut();
        Func<Task> act = () => sut.DeleteAsync(42, 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListByRaffleAsync_ShouldMapAll()
    {
        _awardRepo.Setup(a => a.ListByRaffleAsync(55))
            .ReturnsAsync(new List<RaffleAward>
            {
                new() { RaffleAwardId = 1, RaffleId = 55, Position = 1 },
                new() { RaffleAwardId = 2, RaffleId = 55, Position = 2 }
            });

        var sut = CreateSut();
        var list = await sut.ListByRaffleAsync(55);

        list.Should().HaveCount(2);
    }
}
