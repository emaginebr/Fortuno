using Fortuno.DTO.ProxyPay;
using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Purchase;
using Fortuno.DTO.Webhook;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

/// <summary>
/// Cobertura adicional do PurchaseService (ConfirmAsync, ProcessPaidWebhookAsync,
/// ticket min/max, validação de UserPicks e DrawRandom em pool pequeno).
/// </summary>
public class PurchaseServiceExtendedTests
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

    private static Lottery OpenLottery() => new()
    {
        LotteryId = 1,
        StoreId = 10,
        Status = LotteryStatus.Open,
        NumberType = NumberType.Int64,
        TicketPrice = 10m,
        TicketMin = 0,
        TicketMax = 0,
        NumberValueMin = 1,
        NumberValueMax = 100
    };

    // ---------- Preview error paths ----------

    [Fact]
    public async Task PreviewAsync_ShouldThrow_WhenBelowTicketMin()
    {
        var lottery = OpenLottery(); lottery.TicketMin = 5;
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lottery);

        var sut = CreateSut();
        Func<Task> act = () => sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 2 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mínima*");
    }

    [Fact]
    public async Task PreviewAsync_ShouldThrow_WhenAboveTicketMax()
    {
        var lottery = OpenLottery(); lottery.TicketMax = 3;
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lottery);

        var sut = CreateSut();
        Func<Task> act = () => sut.PreviewAsync(42, new PurchasePreviewRequest { LotteryId = 1, Quantity = 10 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*máxima*");
    }

    // ---------- ConfirmAsync ----------

    [Fact]
    public async Task ConfirmAsync_Random_ShouldCreateInvoiceAndReturnPixInfo()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 2)).ReturnsAsync((LotteryCombo?)null);
        _proxyPay.Setup(p => p.CreateInvoiceAsync(It.IsAny<ProxyPayCreateInvoiceRequest>()))
            .ReturnsAsync(new ProxyPayInvoiceInfo
            {
                InvoiceId = 999,
                PixCopyPaste = "pix-code",
                Status = "pending",
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });

        var sut = CreateSut();
        var result = await sut.ConfirmAsync(42, new PurchaseConfirmRequest
        {
            LotteryId = 1,
            Quantity = 2,
            Mode = PurchaseAssignmentModeDto.Random
        });

        result.InvoiceId.Should().Be(999);
        result.PixCopyPaste.Should().Be("pix-code");
    }

    [Fact]
    public async Task ConfirmAsync_UserPicks_ShouldReserveNumbersAndLinkInvoice()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 2)).ReturnsAsync((LotteryCombo?)null);
        _reservationRepo.Setup(r => r.AreNumbersAvailableAsync(1, It.IsAny<IEnumerable<long>>()))
            .ReturnsAsync(true);
        _reservationRepo.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<NumberReservation>>()))
            .ReturnsAsync((IEnumerable<NumberReservation> res) => res.ToList());
        _reservationRepo.Setup(r => r.ListByUserAndLotteryAsync(42, 1))
            .ReturnsAsync(new List<NumberReservation>
            {
                new() { LotteryId = 1, UserId = 42, TicketNumber = 10, ExpiresAt = DateTime.UtcNow.AddMinutes(15) },
                new() { LotteryId = 1, UserId = 42, TicketNumber = 20, ExpiresAt = DateTime.UtcNow.AddMinutes(15) }
            });
        _reservationRepo.Setup(r => r.UpdateAsync(It.IsAny<NumberReservation>()))
            .ReturnsAsync((NumberReservation n) => n);
        _proxyPay.Setup(p => p.CreateInvoiceAsync(It.IsAny<ProxyPayCreateInvoiceRequest>()))
            .ReturnsAsync(new ProxyPayInvoiceInfo { InvoiceId = 555 });

        var sut = CreateSut();
        var result = await sut.ConfirmAsync(42, new PurchaseConfirmRequest
        {
            LotteryId = 1,
            Quantity = 2,
            Mode = PurchaseAssignmentModeDto.UserPicks,
            PickedNumbers = new List<long> { 10, 20 }
        });

        result.InvoiceId.Should().Be(555);
        _reservationRepo.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<NumberReservation>>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_UserPicks_ShouldThrow_WhenNumbersDuplicated()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 2)).ReturnsAsync((LotteryCombo?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.ConfirmAsync(42, new PurchaseConfirmRequest
        {
            LotteryId = 1,
            Quantity = 2,
            Mode = PurchaseAssignmentModeDto.UserPicks,
            PickedNumbers = new List<long> { 10, 10 }
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicad*");
    }

    [Fact]
    public async Task ConfirmAsync_UserPicks_ShouldThrow_WhenNumberOutOfRange()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 1)).ReturnsAsync((LotteryCombo?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.ConfirmAsync(42, new PurchaseConfirmRequest
        {
            LotteryId = 1,
            Quantity = 1,
            Mode = PurchaseAssignmentModeDto.UserPicks,
            PickedNumbers = new List<long> { 9999 }
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*fora da faixa*");
    }

    [Fact]
    public async Task ConfirmAsync_UserPicks_ShouldThrow_WhenCountMismatch()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(100);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 2)).ReturnsAsync((LotteryCombo?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.ConfirmAsync(42, new PurchaseConfirmRequest
        {
            LotteryId = 1,
            Quantity = 2,
            Mode = PurchaseAssignmentModeDto.UserPicks,
            PickedNumbers = new List<long> { 10 }
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ConfirmAsync_ShouldThrow_WhenExceedsAvailable()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 100)).Returns(2);
        _ticketRepo.Setup(t => t.CountSoldAsync(1)).ReturnsAsync(0);
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _comboRepo.Setup(c => c.FindMatchingComboAsync(1, 10)).ReturnsAsync((LotteryCombo?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.ConfirmAsync(42, new PurchaseConfirmRequest
        {
            LotteryId = 1,
            Quantity = 10,
            Mode = PurchaseAssignmentModeDto.Random
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---------- ProcessPaidWebhookAsync ----------

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldIgnoreNonInvoicePaidEvent()
    {
        var sut = CreateSut();
        await sut.ProcessPaidWebhookAsync(new ProxyPayWebhookPayload { EventType = "invoice.created", Tenant = "fortuna" });

        _webhookRepo.Verify(w => w.ExistsAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldRejectInvalidTenant()
    {
        var payload = new ProxyPayWebhookPayload
        {
            EventType = "invoice.paid",
            Tenant = "outro"
        };

        var sut = CreateSut();
        Func<Task> act = () => sut.ProcessPaidWebhookAsync(payload);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldSkip_WhenAlreadyProcessed()
    {
        var payload = new ProxyPayWebhookPayload
        {
            EventType = "invoice.paid",
            Tenant = "fortuna",
            Data = new ProxyPayWebhookData { InvoiceId = 10 }
        };
        _webhookRepo.Setup(w => w.ExistsAsync(10, "invoice.paid")).ReturnsAsync(true);

        var sut = CreateSut();
        await sut.ProcessPaidWebhookAsync(payload);

        _ticketRepo.Verify(t => t.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldCreateTickets_WhenRandomMode()
    {
        var payload = new ProxyPayWebhookPayload
        {
            EventType = "invoice.paid",
            Tenant = "fortuna",
            Data = new ProxyPayWebhookData
            {
                InvoiceId = 10,
                Amount = 20m,
                Metadata = new Dictionary<string, string>
                {
                    ["fortunoLotteryId"] = "1",
                    ["fortunoUserId"] = "42",
                    ["fortunoQuantity"] = "2",
                    ["fortunoMode"] = "1"  // Random
                }
            }
        };
        _webhookRepo.Setup(w => w.ExistsAsync(10, "invoice.paid")).ReturnsAsync(false);
        _webhookRepo.Setup(w => w.InsertIfNotExistsAsync(It.IsAny<WebhookEvent>()))
            .ReturnsAsync((WebhookEvent e) => e);
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _ticketRepo.Setup(t => t.ListSoldNumbersAsync(1)).ReturnsAsync(new List<long>());
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _ticketRepo.Setup(t => t.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()))
            .ReturnsAsync((IEnumerable<Ticket> t) => t.ToList());

        var sut = CreateSut();
        await sut.ProcessPaidWebhookAsync(payload);

        _ticketRepo.Verify(t => t.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldSkip_WhenLotteryClosed()
    {
        var payload = new ProxyPayWebhookPayload
        {
            EventType = "invoice.paid",
            Tenant = "fortuna",
            Data = new ProxyPayWebhookData
            {
                InvoiceId = 10,
                Metadata = new Dictionary<string, string>
                {
                    ["fortunoLotteryId"] = "1",
                    ["fortunoUserId"] = "42",
                    ["fortunoQuantity"] = "1",
                    ["fortunoMode"] = "1"
                }
            }
        };
        _webhookRepo.Setup(w => w.ExistsAsync(10, "invoice.paid")).ReturnsAsync(false);
        _webhookRepo.Setup(w => w.InsertIfNotExistsAsync(It.IsAny<WebhookEvent>()))
            .ReturnsAsync((WebhookEvent e) => e);
        _lotteryRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Lottery { LotteryId = 1, Status = LotteryStatus.Closed });

        var sut = CreateSut();
        await sut.ProcessPaidWebhookAsync(payload);

        _ticketRepo.Verify(t => t.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldSkip_WhenMetadataMissing()
    {
        var payload = new ProxyPayWebhookPayload
        {
            EventType = "invoice.paid",
            Tenant = "fortuna",
            Data = new ProxyPayWebhookData { InvoiceId = 10 }
        };
        _webhookRepo.Setup(w => w.ExistsAsync(10, "invoice.paid")).ReturnsAsync(false);
        _webhookRepo.Setup(w => w.InsertIfNotExistsAsync(It.IsAny<WebhookEvent>()))
            .ReturnsAsync((WebhookEvent e) => e);

        var sut = CreateSut();
        await sut.ProcessPaidWebhookAsync(payload);

        _ticketRepo.Verify(t => t.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaidWebhookAsync_ShouldRegisterReferrer_WhenCodeResolved()
    {
        var payload = new ProxyPayWebhookPayload
        {
            EventType = "invoice.paid",
            Tenant = "fortuna",
            Data = new ProxyPayWebhookData
            {
                InvoiceId = 10,
                Metadata = new Dictionary<string, string>
                {
                    ["fortunoLotteryId"] = "1",
                    ["fortunoUserId"] = "42",
                    ["fortunoQuantity"] = "1",
                    ["fortunoMode"] = "1",
                    ["fortunoReferralCode"] = "ABC12345"
                }
            }
        };
        _webhookRepo.Setup(w => w.ExistsAsync(10, "invoice.paid")).ReturnsAsync(false);
        _webhookRepo.Setup(w => w.InsertIfNotExistsAsync(It.IsAny<WebhookEvent>()))
            .ReturnsAsync((WebhookEvent e) => e);
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenLottery());
        _ticketRepo.Setup(t => t.ListSoldNumbersAsync(1)).ReturnsAsync(new List<long>());
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(1)).ReturnsAsync(new List<long>());
        _ticketRepo.Setup(t => t.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()))
            .ReturnsAsync((IEnumerable<Ticket> t) => t.ToList());
        _referrer.Setup(r => r.ResolveReferrerUserIdAsync("ABC12345")).ReturnsAsync(99L);

        var sut = CreateSut();
        await sut.ProcessPaidWebhookAsync(payload);

        _invoiceReferrerRepo.Verify(r => r.InsertAsync(
            It.Is<InvoiceReferrer>(ir => ir.ReferrerUserId == 99 && ir.InvoiceId == 10)), Times.Once);
    }
}
