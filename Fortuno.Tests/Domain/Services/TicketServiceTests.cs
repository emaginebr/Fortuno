using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Enums;
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

    public TicketServiceTests()
    {
        // Default TryParse/Format/IsValid mirroring the real NumberCompositionService
        // for Int64 — composed tests set up explicitly quando necessário.
        _numbers.Setup(n => n.TryParse(NumberType.Int64, It.IsAny<string>(), out It.Ref<long>.IsAny))
            .Returns(new TryParseLongHandler((NumberType _, string s, out long v) => long.TryParse(s, out v)));
        _numbers.Setup(n => n.Format(NumberType.Int64, It.IsAny<long>()))
            .Returns((NumberType _, long v) => v.ToString());
        _numbers.Setup(n => n.IsValid(NumberType.Int64, It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .Returns((NumberType _, long v, int min, int max) => v >= min && v <= max);

        // Default vazio para ListByOrderIdAsync — fluxo de pagamento sempre consulta
        // os números pré-escolhidos (ausência de picks → tudo aleatório).
        _orderNumberRepo.Setup(r => r.ListByOrderIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<TicketOrderNumber>());
    }

    private delegate bool TryParseLongHandler(NumberType type, string input, out long value);

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
        _ticketRepo.Setup(r => r.SearchByUserAsync(42, null, null, null, null, 1, 20))
            .ReturnsAsync((tickets, 1L));

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery());

        result.Items.Should().HaveCount(1);
        result.Items[0].TicketId.Should().Be(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListForUserAsync_ShouldNormalizeComposedNumberFilter()
    {
        // Entrada "60-39-05-28-11" deve ser normalizada para "05-11-28-39-60"
        // antes de ser enviada ao repositório (match contra ticket_value).
        _ticketRepo.Setup(r => r.SearchByUserAsync(42, 3L, "05-11-28-39-60", null, null, 2, 50))
            .ReturnsAsync((new List<Ticket>(), 0L));

        var sut = CreateSut();
        var result = await sut.ListForUserAsync(42, new TicketSearchQuery
        {
            LotteryId = 3,
            Number = "60-39-05-28-11",
            Page = 2,
            PageSize = 50
        });

        result.Items.Should().BeEmpty();
        _ticketRepo.Verify(r => r.SearchByUserAsync(42, 3L, "05-11-28-39-60", null, null, 2, 50), Times.Once);
    }

    [Fact]
    public async Task ListForUserAsync_ShouldPassInt64NumberFilter()
    {
        _ticketRepo.Setup(r => r.SearchByUserAsync(42, 3L, "42", null, null, 1, 20))
            .ReturnsAsync((new List<Ticket>(), 0L));

        var sut = CreateSut();
        await sut.ListForUserAsync(42, new TicketSearchQuery
        {
            LotteryId = 3,
            Number = "42"
        });

        _ticketRepo.Verify(r => r.SearchByUserAsync(42, 3L, "42", null, null, 1, 20), Times.Once);
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
            Quantity = 3
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

    // ---------- ReserveNumberAsync ----------

    private static Lottery OpenInt64Lottery() => new()
    {
        LotteryId = 1, StoreId = 99, StoreClientId = "c1", Status = LotteryStatus.Open,
        TicketPrice = 1m, NumberType = NumberType.Int64,
        NumberValueMin = 1, NumberValueMax = 1000,
        TicketNumIni = 1, TicketNumEnd = 1000
    };

    [Fact]
    public async Task ReserveNumberAsync_ShouldReturnReserved_WhenAvailable()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenInt64Lottery());
        _ticketRepo.Setup(r => r.GetByLotteryAndNumberAsync(1, 42)).ReturnsAsync((Ticket?)null);
        _reservationRepo.Setup(r => r.IsNumberReservedAsync(1, 42)).ReturnsAsync(false);
        _reservationRepo.Setup(r => r.InsertBatchAsync(It.IsAny<IEnumerable<NumberReservation>>()))
            .ReturnsAsync((IEnumerable<NumberReservation> list) => list.ToList());

        var sut = CreateSut();
        var result = await sut.ReserveNumberAsync(77, new NumberReservationRequest
        {
            LotteryId = 1,
            TicketNumber = "42"
        });

        result.Success.Should().BeTrue();
        result.Status.Should().Be(NumberReservationStatusDto.Reserved);
        result.TicketNumber.Should().Be("42");
        result.ExpiresAt.Should().NotBeNull();
        result.ExpiresAt!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));

        _reservationRepo.Verify(r => r.InsertBatchAsync(
            It.Is<IEnumerable<NumberReservation>>(list => list.Single().TicketNumber == 42 && list.Single().UserId == 77)),
            Times.Once);
    }

    [Fact]
    public async Task ReserveNumberAsync_ShouldReturnAlreadyPurchased_WhenNumberSold()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenInt64Lottery());
        _ticketRepo.Setup(r => r.GetByLotteryAndNumberAsync(1, 42))
            .ReturnsAsync(new Ticket { TicketId = 10, LotteryId = 1, TicketNumber = 42, UserId = 55 });

        var sut = CreateSut();
        var result = await sut.ReserveNumberAsync(77, new NumberReservationRequest
        {
            LotteryId = 1,
            TicketNumber = "42"
        });

        result.Success.Should().BeFalse();
        result.Status.Should().Be(NumberReservationStatusDto.AlreadyPurchased);
        result.Message.Should().Contain("já foi comprado");
        _reservationRepo.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<NumberReservation>>()), Times.Never);
    }

    [Fact]
    public async Task ReserveNumberAsync_ShouldReturnAlreadyReserved_WhenReservationActive()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenInt64Lottery());
        _ticketRepo.Setup(r => r.GetByLotteryAndNumberAsync(1, 42)).ReturnsAsync((Ticket?)null);
        _reservationRepo.Setup(r => r.IsNumberReservedAsync(1, 42)).ReturnsAsync(true);

        var sut = CreateSut();
        var result = await sut.ReserveNumberAsync(77, new NumberReservationRequest
        {
            LotteryId = 1,
            TicketNumber = "42"
        });

        result.Success.Should().BeFalse();
        result.Status.Should().Be(NumberReservationStatusDto.AlreadyReserved);
        result.Message.Should().Contain("já está reservado");
        _reservationRepo.Verify(r => r.InsertBatchAsync(It.IsAny<IEnumerable<NumberReservation>>()), Times.Never);
    }

    [Fact]
    public async Task ReserveNumberAsync_ShouldThrow_WhenLotteryNotFound()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.ReserveNumberAsync(77, new NumberReservationRequest
        {
            LotteryId = 99,
            TicketNumber = "42"
        });

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ReserveNumberAsync_ShouldThrow_WhenLotteryNotOpen()
    {
        var draft = OpenInt64Lottery();
        draft.Status = LotteryStatus.Draft;
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(draft);

        var sut = CreateSut();
        Func<Task> act = () => sut.ReserveNumberAsync(77, new NumberReservationRequest
        {
            LotteryId = 1,
            TicketNumber = "42"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Open*");
    }

    [Fact]
    public async Task ReserveNumberAsync_ShouldThrow_WhenNumberOutOfRange()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(OpenInt64Lottery());

        var sut = CreateSut();
        Func<Task> act = () => sut.ReserveNumberAsync(77, new NumberReservationRequest
        {
            LotteryId = 1,
            TicketNumber = "9999"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*fora da faixa*");
    }
}
