using Fortuno.DTO.NAuth;
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

public class RaffleServiceTests
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

    // ---------- Create ----------
    [Fact]
    public async Task CreateAsync_ShouldInsertWhenLotteryIsDraft()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = LotteryStatus.Draft });
        _raffleRepo.Setup(r => r.InsertAsync(It.IsAny<Raffle>()))
            .ReturnsAsync((Raffle r) => { r.RaffleId = 99; return r; });

        var sut = CreateSut();
        var info = await sut.CreateAsync(42, new RaffleInsertInfo
        {
            LotteryId = 1,
            Name = "Sorteio 1",
            RaffleDatetime = DateTime.UtcNow.AddDays(1)
        });

        info.RaffleId.Should().Be(99);
        info.Status.Should().Be(RaffleStatusDto.Open);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenLotteryNotDraft()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = LotteryStatus.Open });

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new RaffleInsertInfo { LotteryId = 1, Name = "Rifa" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenLotteryMissing()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, new RaffleInsertInfo { LotteryId = 99 });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ---------- Close ----------
    [Fact]
    public async Task CloseAsync_ShouldTransitionOpenToClosed()
    {
        SetupContext(raffleStatus: RaffleStatus.Open, lotteryStatus: LotteryStatus.Open);
        _raffleRepo.Setup(r => r.UpdateAsync(It.IsAny<Raffle>())).ReturnsAsync((Raffle r) => r);

        var sut = CreateSut();
        var info = await sut.CloseAsync(42, 55);

        info.Status.Should().Be(RaffleStatusDto.Closed);
    }

    [Fact]
    public async Task CloseAsync_ShouldThrow_WhenNotOpen()
    {
        SetupContext(raffleStatus: RaffleStatus.Closed, lotteryStatus: LotteryStatus.Closed);

        var sut = CreateSut();
        Func<Task> act = () => sut.CloseAsync(42, 55);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---------- Preview / Confirm ----------
    [Fact]
    public async Task PreviewWinnersAsync_ShouldThrow_WhenNoNumbers()
    {
        SetupContext(raffleStatus: RaffleStatus.Open, awards: new List<RaffleAward>
        {
            new() { RaffleAwardId = 1, Position = 1, Description = "1º" }
        });
        _winnerRepo.Setup(w => w.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleWinner>());

        var sut = CreateSut();
        Func<Task> act = () => sut.PreviewWinnersAsync(42, new RaffleWinnersPreviewRequest
        {
            RaffleId = 55,
            WinningNumbers = new List<long>()
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PreviewWinnersAsync_ShouldReturnRowsForEachNumber()
    {
        SetupContext(raffleStatus: RaffleStatus.Open, awards: new List<RaffleAward>
        {
            new() { RaffleAwardId = 1, Position = 1, Description = "1º" },
            new() { RaffleAwardId = 2, Position = 2, Description = "2º" }
        });
        _winnerRepo.Setup(w => w.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleWinner>());
        _winnerRepo.Setup(w => w.ListTicketIdsAlreadyWonInLotteryAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<long>());
        _ticketRepo.Setup(t => t.GetByLotteryAndNumberAsync(It.IsAny<long>(), 10))
            .ReturnsAsync(new Ticket { TicketId = 100, UserId = 1, TicketNumber = 10 });
        _ticketRepo.Setup(t => t.GetByLotteryAndNumberAsync(It.IsAny<long>(), 20))
            .ReturnsAsync(new Ticket { TicketId = 101, UserId = 2, TicketNumber = 20 });
        _nauth.Setup(n => n.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new NAuthUserInfo { UserId = 1, Name = "Fulano" });

        var sut = CreateSut();
        var rows = await sut.PreviewWinnersAsync(42, new RaffleWinnersPreviewRequest
        {
            RaffleId = 55,
            WinningNumbers = new List<long> { 10, 20 }
        });

        rows.Should().HaveCount(2);
        rows[0].TicketId.Should().Be(100);
        rows[1].TicketId.Should().Be(101);
    }

    [Fact]
    public async Task PreviewWinnersAsync_ShouldMarkExcluded_WhenTicketAlreadyWonAndFlagFalse()
    {
        SetupContext(raffleStatus: RaffleStatus.Open,
            lotteryId: 9,
            includePreviousWinners: false,
            awards: new List<RaffleAward>
            {
                new() { RaffleAwardId = 1, Position = 1, Description = "1º" }
            });
        _winnerRepo.Setup(w => w.ListByRaffleAsync(55)).ReturnsAsync(new List<RaffleWinner>());
        _winnerRepo.Setup(w => w.ListTicketIdsAlreadyWonInLotteryAsync(9))
            .ReturnsAsync(new List<long> { 100 });
        _ticketRepo.Setup(t => t.GetByLotteryAndNumberAsync(9, 10))
            .ReturnsAsync(new Ticket { TicketId = 100, UserId = 1, TicketNumber = 10 });

        var sut = CreateSut();
        var rows = await sut.PreviewWinnersAsync(42, new RaffleWinnersPreviewRequest
        {
            RaffleId = 55,
            WinningNumbers = new List<long> { 10 }
        });

        rows.Should().HaveCount(1);
        rows[0].ExcludedByFlag.Should().BeTrue();
    }

    // ---------- Helpers ----------

    private void SetupContext(
        RaffleStatus raffleStatus = RaffleStatus.Open,
        LotteryStatus lotteryStatus = LotteryStatus.Open,
        long lotteryId = 1,
        bool includePreviousWinners = true,
        List<RaffleAward>? awards = null)
    {
        _raffleRepo.Setup(r => r.GetByIdAsync(55))
            .ReturnsAsync(new Raffle
            {
                RaffleId = 55,
                LotteryId = lotteryId,
                Status = raffleStatus,
                IncludePreviousWinners = includePreviousWinners
            });
        _lotteryRepo.Setup(r => r.GetByIdAsync(lotteryId))
            .ReturnsAsync(new Lottery { LotteryId = lotteryId, StoreId = 10, Status = lotteryStatus });
        _awardRepo.Setup(a => a.ListByRaffleAsync(55))
            .ReturnsAsync(awards ?? new List<RaffleAward>());
    }
}
