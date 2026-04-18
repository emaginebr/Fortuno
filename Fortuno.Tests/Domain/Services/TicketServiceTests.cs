using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Ticket;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();

    private TicketService CreateSut() => new(_ticketRepo.Object);

    [Fact]
    public async Task ListForUserAsync_ShouldReturnMappedTickets()
    {
        var tickets = new List<Ticket>
        {
            new() { TicketId = 1, UserId = 42, LotteryId = 5, TicketNumber = 10, CreatedAt = DateTime.UtcNow }
        };
        _ticketRepo.Setup(r => r.ListByUserAsync(42, null, null)).ReturnsAsync(tickets);

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery());

        result.Should().HaveCount(1);
        result[0].TicketId.Should().Be(1);
    }

    [Fact]
    public async Task ListForUserAsync_ShouldApplyDateRangeFilter()
    {
        var tickets = new List<Ticket>
        {
            new() { TicketId = 1, UserId = 42, CreatedAt = new DateTime(2025, 1, 1) },
            new() { TicketId = 2, UserId = 42, CreatedAt = new DateTime(2025, 6, 1) },
            new() { TicketId = 3, UserId = 42, CreatedAt = new DateTime(2025, 12, 1) }
        };
        _ticketRepo.Setup(r => r.ListByUserAsync(42, null, null)).ReturnsAsync(tickets);

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery
        {
            FromDate = new DateTime(2025, 3, 1),
            ToDate = new DateTime(2025, 9, 1)
        });

        result.Should().HaveCount(1);
        result[0].TicketId.Should().Be(2);
    }

    [Fact]
    public async Task ListForUserAsync_ShouldPassFiltersToRepository()
    {
        _ticketRepo.Setup(r => r.ListByUserAsync(42, 3, 7)).ReturnsAsync(new List<Ticket>());

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery { LotteryId = 3, Number = 7 });

        result.Should().BeEmpty();
        _ticketRepo.Verify(r => r.ListByUserAsync(42, 3, 7), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTicket_WhenOwner()
    {
        _ticketRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Ticket { TicketId = 1, UserId = 42, RefundState = TicketRefundState.None });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(1, 42);

        result.Should().NotBeNull();
        result!.TicketId.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotOwner()
    {
        _ticketRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Ticket { TicketId = 1, UserId = 99 });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(1, 42);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        _ticketRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Ticket?)null);

        var sut = CreateSut();
        (await sut.GetByIdAsync(1, 42)).Should().BeNull();
    }
}
