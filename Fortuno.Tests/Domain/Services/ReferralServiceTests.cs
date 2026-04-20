using Fortuno.DTO.NAuth;
using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class ReferralServiceTests
{
    private readonly Mock<IInvoiceReferrerRepository<InvoiceReferrer>> _invoiceReferrerRepo = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();
    private readonly Mock<IUserReferrerRepository<UserReferrer>> _userReferrerRepo = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();
    private readonly Mock<INAuthAppService> _nauth = new();
    private readonly Mock<IUserReferrerService> _referrerService = new();

    private ReferralService CreateSut() => new(
        _invoiceReferrerRepo.Object,
        _lotteryRepo.Object,
        _ticketRepo.Object,
        _userReferrerRepo.Object,
        _ownership.Object,
        _nauth.Object,
        _referrerService.Object);

    [Fact]
    public async Task GetEarningsForReferrerAsync_ShouldAggregateInvoices()
    {
        _referrerService.Setup(s => s.GetOrCreateCodeForUserAsync(42)).ReturnsAsync("ABC12345");
        _invoiceReferrerRepo.Setup(r => r.ListByReferrerAsync(42))
            .ReturnsAsync(new List<InvoiceReferrer>
            {
                new() { InvoiceId = 1, LotteryId = 10, ReferrerUserId = 42 },
                new() { InvoiceId = 2, LotteryId = 10, ReferrerUserId = 42 }
            });
        _ticketRepo.Setup(t => t.ListByInvoiceAsync(1))
            .ReturnsAsync(new List<Ticket> { new() { TicketId = 1, RefundState = TicketRefundState.None } });
        _ticketRepo.Setup(t => t.ListByInvoiceAsync(2))
            .ReturnsAsync(new List<Ticket> { new() { TicketId = 2, RefundState = TicketRefundState.None } });
        _lotteryRepo.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Lottery { LotteryId = 10, Name = "Rifa 10", TicketPrice = 50m, ReferralPercent = 10f });

        var sut = CreateSut();
        var panel = await sut.GetEarningsForReferrerAsync(42);

        panel.ReferralCode.Should().Be("ABC12345");
        panel.TotalPurchases.Should().Be(2);
        panel.TotalToReceive.Should().Be(10m); // 2 * 50 * 0.10
        panel.ByLottery.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetEarningsForReferrerAsync_ShouldReturnZero_WhenNoInvoices()
    {
        _referrerService.Setup(s => s.GetOrCreateCodeForUserAsync(42)).ReturnsAsync("ABC12345");
        _invoiceReferrerRepo.Setup(r => r.ListByReferrerAsync(42))
            .ReturnsAsync(new List<InvoiceReferrer>());

        var sut = CreateSut();
        var panel = await sut.GetEarningsForReferrerAsync(42);

        panel.TotalPurchases.Should().Be(0);
        panel.TotalToReceive.Should().Be(0m);
        panel.ByLottery.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPayablesForLotteryAsync_ShouldGroupByReferrer()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new Lottery { LotteryId = 10, StoreId = 5, Name = "Rifa", TicketPrice = 100m, ReferralPercent = 5f });
        _invoiceReferrerRepo.Setup(r => r.ListByLotteryAsync(10))
            .ReturnsAsync(new List<InvoiceReferrer>
            {
                new() { InvoiceId = 1, LotteryId = 10, ReferrerUserId = 42 },
                new() { InvoiceId = 2, LotteryId = 10, ReferrerUserId = 43 }
            });
        _ticketRepo.Setup(t => t.ListByInvoiceAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<Ticket> { new() { RefundState = TicketRefundState.None } });
        _nauth.Setup(n => n.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new NAuthUserInfo { UserId = 42, Name = "Usuário" });
        _userReferrerRepo.Setup(r => r.GetByUserIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new UserReferrer { UserId = 42, ReferralCode = "CODE1234" });

        var sut = CreateSut();
        var panel = await sut.GetPayablesForLotteryAsync(5, 10);

        panel.LotteryId.Should().Be(10);
        panel.ByReferrer.Should().HaveCount(2);
        panel.TotalPayable.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPayablesForLotteryAsync_ShouldThrow_WhenLotteryMissing()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(It.IsAny<long>())).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.GetPayablesForLotteryAsync(5, 10);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
