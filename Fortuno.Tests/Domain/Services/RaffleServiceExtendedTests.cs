using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Raffle;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

/// <summary>
/// Cobertura adicional do RaffleService (Update, Delete, Cancel, ConfirmWinners).
/// </summary>
public class RaffleServiceExtendedTests
{
    private readonly Mock<IRaffleRepository<Raffle>> _raffleRepo = new();
    private readonly Mock<IRaffleAwardRepository<RaffleAward>> _awardRepo = new();
    private readonly Mock<IRaffleWinnerRepository<RaffleWinner>> _winnerRepo = new();
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();
    private readonly Mock<INAuthAppService> _nauth = new();

    private RaffleService CreateSut() => new(
        _raffleRepo.Object,
        _awardRepo.Object,
        _winnerRepo.Object,
        _ticketRepo.Object,
        _lotteryRepo.Object,
        _ownership.Object,
        _nauth.Object);

    private void SetupRaffleAndLottery(RaffleStatus raffleStatus, LotteryStatus lotteryStatus)
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(55))
            .ReturnsAsync(new Raffle { RaffleId = 55, LotteryId = 1, Status = raffleStatus });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = lotteryStatus });
        _awardRepo.Setup(a => a.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleAward>());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Raffle?)null);
        var sut = CreateSut();

        (await sut.GetByIdAsync(99)).Should().BeNull();
    }

    [Fact]
    public async Task ListByLotteryAsync_ShouldMapAll()
    {
        _raffleRepo.Setup(r => r.ListByLotteryAsync(1, null))
            .ReturnsAsync(new List<Raffle>
            {
                new() { RaffleId = 1, LotteryId = 1, Status = RaffleStatus.Open },
                new() { RaffleId = 2, LotteryId = 1, Status = RaffleStatus.Closed }
            });

        var sut = CreateSut();
        (await sut.ListByLotteryAsync(1)).Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersist_WhenLotteryDraft()
    {
        SetupRaffleAndLottery(RaffleStatus.Open, LotteryStatus.Draft);
        _raffleRepo.Setup(r => r.UpdateAsync(It.IsAny<Raffle>())).ReturnsAsync((Raffle r) => r);

        var sut = CreateSut();
        var info = await sut.UpdateAsync(42, 55, new RaffleUpdateInfo
        {
            LotteryId = 1,
            Name = "Atualizado",
            RaffleDatetime = DateTime.UtcNow.AddDays(2)
        });

        info.Name.Should().Be("Atualizado");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        SetupRaffleAndLottery(RaffleStatus.Open, LotteryStatus.Open);

        var sut = CreateSut();
        Func<Task> act = () => sut.UpdateAsync(42, 55, new RaffleUpdateInfo { LotteryId = 1, Name = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldInvokeRepo_WhenLotteryDraft()
    {
        SetupRaffleAndLottery(RaffleStatus.Open, LotteryStatus.Draft);

        var sut = CreateSut();
        await sut.DeleteAsync(42, 55);

        _raffleRepo.Verify(r => r.DeleteAsync(55), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        SetupRaffleAndLottery(RaffleStatus.Open, LotteryStatus.Open);

        var sut = CreateSut();
        Func<Task> act = () => sut.DeleteAsync(42, 55);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CancelAsync_ShouldSimplyCancel_WhenNoTicketsSold()
    {
        SetupRaffleAndLottery(RaffleStatus.Open, LotteryStatus.Open);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _raffleRepo.Setup(r => r.UpdateAsync(It.IsAny<Raffle>())).ReturnsAsync((Raffle r) => r);

        var sut = CreateSut();
        var info = await sut.CancelAsync(42, 55, new RaffleCancelRequest());

        info.Status.Should().Be(RaffleStatusDto.Cancelled);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenRaffleNotOpen()
    {
        SetupRaffleAndLottery(RaffleStatus.Closed, LotteryStatus.Open);

        var sut = CreateSut();
        Func<Task> act = () => sut.CancelAsync(42, 55, new RaffleCancelRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CancelAsync_ShouldRequireRedistribution_WhenTicketsSold()
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(55))
            .ReturnsAsync(new Raffle { RaffleId = 55, LotteryId = 1, Status = RaffleStatus.Open });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = LotteryStatus.Open });
        _awardRepo.Setup(a => a.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleAward>
        {
            new() { RaffleAwardId = 100, RaffleId = 55, Position = 1 }
        });
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(5);
        _raffleRepo.Setup(r => r.ListByLotteryAsync(1, null))
            .ReturnsAsync(new List<Raffle>
            {
                new() { RaffleId = 55, Status = RaffleStatus.Open },
                new() { RaffleId = 66, Status = RaffleStatus.Open }
            });

        var sut = CreateSut();
        Func<Task> act = () => sut.CancelAsync(42, 55, new RaffleCancelRequest());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*redistribuídos*");
    }

    [Fact]
    public async Task ConfirmWinnersAsync_ShouldPersistWinners_WhenPreviewValid()
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(55))
            .ReturnsAsync(new Raffle { RaffleId = 55, LotteryId = 1, Status = RaffleStatus.Open, IncludePreviousWinners = true });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = LotteryStatus.Open });
        _awardRepo.Setup(a => a.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleAward>
        {
            new() { RaffleAwardId = 1, Position = 1, Description = "1º" }
        });
        _winnerRepo.Setup(w => w.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleWinner>());
        _ticketRepo.Setup(t => t.GetByLotteryAndNumberAsync(1, 10))
            .ReturnsAsync(new Ticket { TicketId = 200, UserId = 7, TicketNumber = 10, LotteryId = 1 });
        _nauth.Setup(n => n.GetByIdAsync(7))
            .ReturnsAsync(new NAuthUserInfo { UserId = 7, Name = "Ganhador" });
        _winnerRepo.Setup(w => w.InsertBatchAsync(It.IsAny<IEnumerable<RaffleWinner>>()))
            .ReturnsAsync((IEnumerable<RaffleWinner> ws) => ws.ToList());
        _ticketRepo.Setup(t => t.GetByIdAsync(200))
            .ReturnsAsync(new Ticket { TicketId = 200, TicketNumber = 10 });

        var sut = CreateSut();
        var winners = await sut.ConfirmWinnersAsync(42, new RaffleWinnersPreviewRequest
        {
            RaffleId = 55,
            WinningNumbers = new List<long> { 10 }
        });

        winners.Should().HaveCount(1);
        winners[0].TicketNumber.Should().Be(10);
    }

    [Fact]
    public async Task ConfirmWinnersAsync_ShouldThrow_WhenRaffleNotOpen()
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(55))
            .ReturnsAsync(new Raffle { RaffleId = 55, LotteryId = 1, Status = RaffleStatus.Closed });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = LotteryStatus.Closed });
        _awardRepo.Setup(a => a.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleAward>());

        var sut = CreateSut();
        Func<Task> act = () => sut.ConfirmWinnersAsync(42, new RaffleWinnersPreviewRequest { RaffleId = 55 });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
