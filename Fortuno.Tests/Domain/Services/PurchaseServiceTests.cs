using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Purchase;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class PurchaseServiceTests
{
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<ILotteryComboRepository<LotteryCombo>> _comboRepo = new();
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();
    private readonly Mock<INumberReservationRepository<NumberReservation>> _reservationRepo = new();
    private readonly Mock<IInvoiceReferrerRepository<InvoiceReferrer>> _invoiceReferrerRepo = new();
    private readonly Mock<IWebhookEventRepository<WebhookEvent>> _webhookRepo = new();
    private readonly Mock<INumberCompositionService> _numbers = new();
    private readonly Mock<IUserReferrerService> _referrer = new();
    private readonly Mock<IProxyPayAppService> _proxyPay = new();

    private PurchaseService CreateSut() => new(
        _lotteryRepo.Object,
        _comboRepo.Object,
        _ticketRepo.Object,
        _reservationRepo.Object,
        _invoiceReferrerRepo.Object,
        _webhookRepo.Object,
        _numbers.Object,
        _referrer.Object,
        _proxyPay.Object);

    private static Lottery OpenLottery(decimal price = 10m) => new()
    {
        LotteryId = 1,
        StoreId = 10,
        Status = LotteryStatus.Open,
        NumberType = NumberType.Int64,
        TicketPrice = price,
        TicketMin = 0,
        TicketMax = 0,
        NumberValueMin = 1,
        NumberValueMax = 100
    };

    [Fact]
    public async Task PreviewAsync_ShouldComputeTotalsAndAvailability()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(10);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 3)).ReturnsAsync((LotteryCombo?)null);

        var sut = CreateSut();
        var preview = await sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 3 });

        preview.Quantity.Should().Be(3);
        preview.UnitPrice.Should().Be(10m);
        preview.TotalAmount.Should().Be(30m);
        preview.AvailableTickets.Should().Be(90);
    }

    [Fact]
    public async Task PreviewAsync_ShouldApplyComboDiscount()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 5))
            .ReturnsAsync(new LotteryCombo { Name = "5-pack", DiscountValue = 10f });

        var sut = CreateSut();
        var preview = await sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 5 });

        preview.DiscountValue.Should().Be(5m);   // 10% de 50
        preview.TotalAmount.Should().Be(45m);
        preview.ApplicableCombo.Should().Be("5-pack");
    }

    [Fact]
    public async Task PreviewAsync_ShouldThrow_WhenLotteryNotOpen()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, Status = LotteryStatus.Draft });

        var sut = CreateSut();
        Func<Task> act = () => sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 1 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Open*");
    }

    [Fact]
    public async Task PreviewAsync_ShouldThrow_WhenQuantityNonPositive()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());

        var sut = CreateSut();
        Func<Task> act = () => sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 0 });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PreviewAsync_ShouldThrow_WhenLotteryNotFound()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 1 });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task PreviewAsync_ShouldMarkReferrerSelf_WhenReferralCodeResolvesToCurrentUser()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 1)).ReturnsAsync((LotteryCombo?)null);
        _referrer.Setup(r => r.ResolveReferrerUserIdAsync("ABC12345")).ReturnsAsync(42L);

        var sut = CreateSut();
        var preview = await sut.PreviewAsync(42, new PurchasePreviewRequest
        {
            LotteryId = 1,
            Quantity = 1,
            ReferralCode = "ABC12345"
        });

        preview.ReferrerIsSelf.Should().BeTrue();
        preview.ReferrerUserId.Should().BeNull();
    }
}
