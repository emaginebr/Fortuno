using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Refund;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class RefundServiceTests
{
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<IRefundLogRepository<RefundLog>> _logRepo = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();

    private RefundService CreateSut() =>
        new(_ticketRepo.Object, _lotteryRepo.Object, _logRepo.Object, _ownership.Object);

    [Fact]
    public async Task ListPendingByLotteryAsync_ShouldReturnOnlyPendingTickets()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, Status = LotteryStatus.Cancelled });
        _ticketRepo.Setup(t => t.ListByLotteryAsync(1)).ReturnsAsync(new List<Ticket>
        {
            new() { TicketId = 1, LotteryId = 1, RefundState = TicketRefundState.PendingRefund },
            new() { TicketId = 2, LotteryId = 1, RefundState = TicketRefundState.None },
            new() { TicketId = 3, LotteryId = 1, RefundState = TicketRefundState.Refunded }
        });

        var sut = CreateSut();
        var list = await sut.ListPendingByLotteryAsync(42, 1);

        list.Should().HaveCount(1);
        list[0].TicketId.Should().Be(1);
    }

    [Fact]
    public async Task ListPendingByLotteryAsync_ShouldThrow_WhenLotteryMissing()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.ListPendingByLotteryAsync(42, 1);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task MarkRefundedAsync_ShouldTransitionEligibleTicketsAndLog()
    {
        _ticketRepo.Setup(t => t.GetByIdAsync(1))
            .ReturnsAsync(new Ticket { TicketId = 1, LotteryId = 1, RefundState = TicketRefundState.PendingRefund });
        _ticketRepo.Setup(t => t.GetByIdAsync(2))
            .ReturnsAsync(new Ticket { TicketId = 2, LotteryId = 1, RefundState = TicketRefundState.None });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10, TicketPrice = 10m });

        var sut = CreateSut();
        var count = await sut.MarkRefundedAsync(42, new RefundStatusChangeRequest
        {
            TicketIds = new List<long> { 1, 2 },
            ExternalReference = "ext-123"
        });

        count.Should().Be(1);
        _ticketRepo.Verify(t => t.MarkRefundStateAsync(
            It.Is<IEnumerable<long>>(ids => ids.Contains(1) && !ids.Contains(2)),
            (int)TicketRefundState.Refunded), Times.Once);
        _logRepo.Verify(l => l.InsertBatchAsync(It.Is<IEnumerable<RefundLog>>(logs => logs.Count() == 1)), Times.Once);
    }

    [Fact]
    public async Task MarkRefundedAsync_ShouldThrow_WhenNoTicketIds()
    {
        var sut = CreateSut();
        Func<Task> act = () => sut.MarkRefundedAsync(42, new RefundStatusChangeRequest());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MarkRefundedAsync_ShouldThrow_WhenNoValidTicketsFound()
    {
        _ticketRepo.Setup(t => t.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Ticket?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.MarkRefundedAsync(42, new RefundStatusChangeRequest
        {
            TicketIds = new List<long> { 999 }
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*válido*");
    }

    [Fact]
    public async Task MarkRefundedAsync_ShouldReturnZero_WhenNoneEligible()
    {
        _ticketRepo.Setup(t => t.GetByIdAsync(1))
            .ReturnsAsync(new Ticket { TicketId = 1, LotteryId = 1, RefundState = TicketRefundState.None });
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, StoreId = 10 });

        var sut = CreateSut();
        var count = await sut.MarkRefundedAsync(42, new RefundStatusChangeRequest
        {
            TicketIds = new List<long> { 1 }
        });

        count.Should().Be(0);
    }
}
