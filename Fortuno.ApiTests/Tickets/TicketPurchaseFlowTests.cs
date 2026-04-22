using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.LotteryImage;
using Fortuno.DTO.Raffle;
using Fortuno.DTO.RaffleAward;
using Fortuno.DTO.Ticket;

namespace Fortuno.ApiTests.Tickets;

[Collection("api")]
public class TicketPurchaseFlowTests
{
    private const string Tiny1x1Png =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    private readonly ApiSessionFixture _fixture;

    public TicketPurchaseFlowTests(ApiSessionFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_Random_ShouldReturnQRCodeInfo()
    {
        var lottery = await CreateOpenLotteryAsync();

        var request = new TicketOrderRequest
        {
            LotteryId = lottery.LotteryId,
            Quantity = 2,
            Mode = TicketOrderMode.Random
        };

        IFlurlResponse response;
        try
        {
            response = await _fixture.Client
                .Request("tickets", "qrcode")
                .PostJsonAsync(request);
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException($"POST /tickets/qrcode falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        response.StatusCode.Should().Be(201);
        var info = await response.GetJsonAsync<TicketQRCodeInfo>();
        info.Should().NotBeNull();
        info.InvoiceId.Should().BeGreaterThan(0);
        info.BrCode.Should().NotBeNullOrEmpty();
        info.BrCodeBase64.Should().NotBeNullOrEmpty();
        // Comparar em UTC explícito para evitar divergência por fuso-horário entre
        // host Windows (UTC-3) e container API (UTC).
        info.ExpiredAt.ToUniversalTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task Status_UnknownInvoice_ShouldReturnUnknown()
    {
        var response = await _fixture.Client
            .Request("tickets", "qrcode", 99999999L, "status")
            .GetJsonAsync<TicketQRCodeStatusInfo>();

        response.Status.Should().BeNull();
    }

    [Fact]
    public async Task PurchaseFlow_Create_StatusPending_SimulatePayment_StatusPaid()
    {
        var lottery = await CreateOpenLotteryAsync();

        // 1) POST /tickets/qrcode { quantity=3, mode=Random }
        TicketQRCodeInfo qr;
        try
        {
            var resp = await _fixture.Client.Request("tickets", "qrcode")
                .PostJsonAsync(new TicketOrderRequest
                {
                    LotteryId = lottery.LotteryId,
                    Quantity = 3,
                    Mode = TicketOrderMode.Random
                });
            qr = await resp.GetJsonAsync<TicketQRCodeInfo>();
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException($"POST /tickets/qrcode falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        qr.InvoiceId.Should().BeGreaterThan(0);

        // 2) GET status antes de pagar — Pending (1)
        var pending = await _fixture.Client
            .Request("tickets", "qrcode", qr.InvoiceId, "status")
            .GetJsonAsync<TicketQRCodeStatusInfo>();
        pending.Status.Should().Be(1); // TicketOrderStatus.Pending
        pending.Tickets.Should().BeNullOrEmpty();

        // 3) Simula pagamento no ProxyPay usando o mesmo token
        var simulateUrl = $"{_fixture.ProxyPayUrl.TrimEnd('/')}/payment/simulate-payment/{qr.InvoiceId}";
        try
        {
            await new FlurlRequest(simulateUrl)
                .WithHeader("Authorization", $"Basic {_fixture.Token}")
                .WithHeader("X-Tenant-Id", _fixture.NAuthTenant)
                .PostAsync();
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"Simulate payment falhou em {simulateUrl} com {ex.StatusCode}. Body: {body}", ex);
        }

        // 4) GET status após pagamento — Paid (3) + tickets emitidos
        var paid = await _fixture.Client
            .Request("tickets", "qrcode", qr.InvoiceId, "status")
            .GetJsonAsync<TicketQRCodeStatusInfo>();
        paid.Status.Should().Be(3); // TicketOrderStatus.Paid
        paid.Tickets.Should().NotBeNullOrEmpty();
        paid.Tickets!.Should().HaveCount(3);
        paid.Tickets.Should().OnlyContain(t => t.TicketId > 0 && t.InvoiceId == qr.InvoiceId);
    }

    [Fact]
    public async Task Create_Random_OnComposed5Lottery_ShouldReturnQRCodeInfo()
    {
        // Lottery com NumberType = Composed5: 5 componentes de 2 dígitos cada (0-99).
        // Pool total = 100^5 mas limitamos via TicketNumIni/TicketNumEnd.
        var lottery = await CreateOpenLotteryAsync(
            NumberTypeDto.Composed5,
            numberValueMin: 0,
            numberValueMax: 99,
            ticketNumIni: 0,
            ticketNumEnd: 9_999_999_999L);

        var request = new TicketOrderRequest
        {
            LotteryId = lottery.LotteryId,
            Quantity = 2,
            Mode = TicketOrderMode.Random
        };

        IFlurlResponse response;
        try
        {
            response = await _fixture.Client
                .Request("tickets", "qrcode")
                .PostJsonAsync(request);
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"POST /tickets/qrcode (Composed5) falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        response.StatusCode.Should().Be(201);
        var info = await response.GetJsonAsync<TicketQRCodeInfo>();
        info.Should().NotBeNull();
        info.InvoiceId.Should().BeGreaterThan(0);
        info.BrCode.Should().NotBeNullOrEmpty();
    }

    private async Task<LotteryInfo> CreateOpenLotteryAsync(
        NumberTypeDto numberType = NumberTypeDto.Int64,
        int numberValueMin = 1,
        int numberValueMax = 1000,
        long ticketNumIni = 1,
        long ticketNumEnd = 1000)
    {
        var lottery = await PostAsync<LotteryInsertInfo, LotteryInfo>("lotteries", new()
        {
            StoreId = _fixture.StoreId,
            Name = UniqueId.New($"qa-ticket-{numberType.ToString().ToLowerInvariant()}"),
            DescriptionMd = "# desc",
            RulesMd = "## regras",
            PrivacyPolicyMd = "## privacy",
            TicketPrice = 10m,
            TotalPrizeValue = 1000m,
            TicketMin = 1,
            TicketMax = 0,
            TicketNumIni = ticketNumIni,
            TicketNumEnd = ticketNumEnd,
            NumberType = numberType,
            NumberValueMin = numberValueMin,
            NumberValueMax = numberValueMax,
            ReferralPercent = 0f
        });

        await PostAsync<LotteryImageInsertInfo, LotteryImageInfo>("lottery-images", new()
        {
            LotteryId = lottery.LotteryId,
            ImageBase64 = Tiny1x1Png,
            Description = "qa",
            DisplayOrder = 0
        });

        var raffle = await PostAsync<RaffleInsertInfo, RaffleInfo>("raffles", new()
        {
            LotteryId = lottery.LotteryId,
            Name = "Sorteio QA",
            DescriptionMd = "desc",
            RaffleDatetime = DateTime.UtcNow.AddDays(30),
            IncludePreviousWinners = false
        });

        await PostAsync<RaffleAwardInsertInfo, RaffleAwardInfo>("raffle-awards", new()
        {
            RaffleId = raffle.RaffleId,
            Position = 1,
            Description = "Prêmio"
        });

        var publishResp = await _fixture.Client
            .Request("lotteries", lottery.LotteryId, "publish")
            .PostAsync();
        publishResp.StatusCode.Should().Be(200);

        return await _fixture.Client
            .Request("lotteries", lottery.LotteryId)
            .GetJsonAsync<LotteryInfo>();
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body)
    {
        try
        {
            var response = await _fixture.Client.Request(path).PostJsonAsync(body);
            return await response.GetJsonAsync<TResponse>();
        }
        catch (FlurlHttpException ex)
        {
            var errBody = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException($"POST /{path} falhou com {ex.StatusCode}. Body: {errBody}", ex);
        }
    }
}
