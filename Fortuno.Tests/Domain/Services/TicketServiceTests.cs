using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.NAuth;
using Fortuno.DTO.ProxyPay;
using Fortuno.DTO.Ticket;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;
using DtoTicketOrderMode = Fortuno.DTO.Enums.TicketOrderMode;

namespace Fortuno.Tests.Domain.Services;

public class TicketServiceTests
{
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<ILotteryComboRepository<LotteryCombo>> _comboRepo = new();
    private readonly Mock<INumberReservationRepository<NumberReservation>> _reservationRepo = new();
    private readonly Mock<ITicketOrderRepository<TicketOrder>> _orderRepo = new();
    private readonly Mock<ITicketOrderNumberRepository<TicketOrderNumber>> _orderNumberRepo = new();
    private readonly Mock<IInvoiceReferrerRepository<InvoiceReferrer>> _invoiceReferrerRepo = new();
    private readonly Mock<INumberCompositionService> _numbers = new();
    private readonly Mock<IUserReferrerService> _referrer = new();
    private readonly Mock<IProxyPayAppService> _proxyPay = new();
    private readonly Mock<INAuthAppService> _nauth = new();

    private TicketService CreateSut() => new(
        _ticketRepo.Object,
        _lotteryRepo.Object,
        _comboRepo.Object,
        _reservationRepo.Object,
        _orderRepo.Object,
        _orderNumberRepo.Object,
        _invoiceReferrerRepo.Object,
        _numbers.Object,
        _referrer.Object,
        _proxyPay.Object,
        _nauth.Object);

    [Fact]
    public async Task ListForUserAsync_ShouldReturnMappedTickets()
    {
        var tickets = new List<Ticket>
        {
            new() { TicketId = 1, UserId = 42, LotteryId = 5, TicketNumber = 10, CreatedAt = DateTime.UtcNow }
        };
        _ticketRepo.Setup(r => r.SearchByUserAsync(42, null, null, null, null, null, 1, 20))
            .ReturnsAsync((tickets, 1L));

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery());

        result.Items.Should().HaveCount(1);
        result.Items[0].TicketId.Should().Be(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListForUserAsync_ShouldPassFiltersToRepository()
    {
        _ticketRepo.Setup(r => r.SearchByUserAsync(42, 3L, 7L, "05-11-28-39-60", null, null, 2, 50))
            .ReturnsAsync((new List<Ticket>(), 0L));

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery
        {
            LotteryId = 3,
            Number = 7,
            TicketValue = "05-11-28-39-60",
            Page = 2,
            PageSize = 50
        });

        result.Items.Should().BeEmpty();
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(50);
        _ticketRepo.Verify(r => r.SearchByUserAsync(42, 3L, 7L, "05-11-28-39-60", null, null, 2, 50), Times.Once);
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

    [Fact]
    public async Task PurchaseFlow_Create_StatusPending_SimulatePayment_StatusPaid()
    {
        // ---------- Setup ----------
        const long userId = 42;
        const long lotteryId = 1;
        const long storeId = 99;
        const long invoiceId = 100;

        var lottery = new Lottery
        {
            LotteryId = lotteryId, StoreId = storeId, StoreClientId = "client-abc",
            Status = LotteryStatus.Open,
            TicketPrice = 10m, NumberType = NumberType.Int64,
            NumberValueMin = 1, NumberValueMax = 1000,
            TicketMin = 1, TicketMax = 10, TicketNumIni = 1, TicketNumEnd = 1000,
            ReferralPercent = 0f
        };
        _lotteryRepo.Setup(r => r.GetByIdAsync(lotteryId)).ReturnsAsync(lottery);

        _nauth.Setup(n => n.GetCurrentAsync()).ReturnsAsync(new NAuthUserInfo
        {
            UserId = userId, Name = "John", Email = "j@e.com",
            DocumentId = "12345678900", Phone = "11999999999"
        });

        _proxyPay.Setup(p => p.GetStoreAsync(storeId)).ReturnsAsync(new ProxyPayStoreInfo
        {
            StoreId = storeId, OwnerUserId = userId, ClientId = "client-abc"
        });

        _proxyPay.Setup(p => p.CreateQRCodeAsync(It.IsAny<ProxyPayQRCodeRequest>()))
            .ReturnsAsync(new ProxyPayQRCodeResponse
            {
                InvoiceId = invoiceId,
                InvoiceNumber = "INV-100",
                BrCode = "00020101...",
                BrCodeBase64 = "data:image/png;base64,AAA",
                ExpiredAt = DateTime.UtcNow.AddMinutes(15)
            });

        _comboRepo.Setup(r => r.FindMatchingComboAsync(lotteryId, 3)).ReturnsAsync((LotteryCombo?)null);

        // Pool: 1000 total, nada vendido, nada reservado
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 1, 1000)).Returns(1000);
        _ticketRepo.Setup(r => r.CountSoldAsync(lotteryId)).ReturnsAsync(0);
        _ticketRepo.Setup(r => r.ListSoldNumbersAsync(lotteryId)).ReturnsAsync(new List<long>());
        _reservationRepo.Setup(r => r.ListActiveReservedNumbersAsync(lotteryId)).ReturnsAsync(new List<long>());
        _numbers.Setup(n => n.Format(NumberType.Int64, It.IsAny<long>()))
            .Returns<NumberType, long>((_, v) => v.ToString());

        // Máquina de estado do TicketOrder (compartilhada entre chamadas)
        TicketOrder? currentOrder = null;
        _orderRepo.Setup(r => r.InsertAsync(It.IsAny<TicketOrder>()))
            .Callback<TicketOrder>(o => { o.TicketOrderId = 1; currentOrder = o; })
            .ReturnsAsync((TicketOrder o) => o);
        _orderRepo.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
            .ReturnsAsync(() => currentOrder);
        _orderRepo.Setup(r => r.TryMarkPaidAsync(1))
            .Callback(() => { if (currentOrder is not null) currentOrder.Status = TicketOrderStatus.Paid; })
            .ReturnsAsync(1);

        // Emissão de tickets (simula INSERT no DB atribuindo IDs)
        var emitted = new List<Ticket>();
        _ticketRepo.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()))
            .Callback<IEnumerable<Ticket>>(ts =>
            {
                var list = ts.ToList();
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].TicketId = emitted.Count + i + 1;
                    list[i].Lottery = lottery;
                }
                emitted.AddRange(list);
            })
            .ReturnsAsync((IEnumerable<Ticket> ts) => ts.ToList());
        _ticketRepo.Setup(r => r.ListByInvoiceAsync(invoiceId)).ReturnsAsync(() => emitted);

        var sut = CreateSut();

        // ---------- Ato 1: POST /tickets/qrcode ----------
        var qr = await sut.CreateQRCodeAsync(userId, new TicketOrderRequest
        {
            LotteryId = lotteryId,
            Quantity = 3,
            Mode = DtoTicketOrderMode.Random
        });

        qr.InvoiceId.Should().Be(invoiceId);
        qr.BrCode.Should().NotBeNullOrEmpty();
        currentOrder.Should().NotBeNull();
        currentOrder!.Status.Should().Be(TicketOrderStatus.Pending);

        // ---------- Ato 2: status antes de pagar (Pending) ----------
        _proxyPay.Setup(p => p.GetQRCodeStatusAsync(invoiceId))
            .ReturnsAsync(new ProxyPayQRCodeStatusResponse
            {
                InvoiceId = invoiceId,
                Status = (int)TicketOrderStatus.Pending
            });

        var pending = await sut.CheckQRCodeStatusAsync(invoiceId);
        pending.Status.Should().Be((int)TicketOrderStatus.Pending);
        pending.Tickets.Should().BeNull();
        emitted.Should().BeEmpty();

        // ---------- Ato 3: simula pagamento (provedor passa a retornar Paid) ----------
        _proxyPay.Setup(p => p.GetQRCodeStatusAsync(invoiceId))
            .ReturnsAsync(new ProxyPayQRCodeStatusResponse
            {
                InvoiceId = invoiceId,
                Status = (int)TicketOrderStatus.Paid
            });

        // ---------- Ato 4: status pós-pagamento (Paid + tickets emitidos) ----------
        var paid = await sut.CheckQRCodeStatusAsync(invoiceId);
        paid.Status.Should().Be((int)TicketOrderStatus.Paid);
        paid.Tickets.Should().NotBeNull();
        paid.Tickets!.Should().HaveCount(3);
        emitted.Should().HaveCount(3);
        currentOrder!.Status.Should().Be(TicketOrderStatus.Paid);

        // ---------- Ato 5: polling idempotente — segunda checagem não re-emite ----------
        var secondCheck = await sut.CheckQRCodeStatusAsync(invoiceId);
        secondCheck.Status.Should().Be((int)TicketOrderStatus.Paid);
        secondCheck.Tickets!.Should().HaveCount(3);
        emitted.Should().HaveCount(3); // ainda 3 — não duplicou
        _ticketRepo.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<Ticket>>()), Times.Once);
        _orderRepo.Verify(r => r.TryMarkPaidAsync(1), Times.Once);
    }
}
