using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.Raffle;
using Fortuno.DTO.RaffleAward;

namespace Fortuno.ApiTests.RaffleAwards;

[Collection("api")]
public class RaffleAwardCreationTests
{
    private readonly ApiSessionFixture _fixture;

    public RaffleAwardCreationTests(ApiSessionFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_ShouldPersistAwardForDraftLotteryRaffle()
    {
        var lottery = await CreateDraftLotteryAsync();
        var raffle = await CreateRaffleAsync(lottery.LotteryId);

        var dto = new RaffleAwardInsertInfo
        {
            RaffleId = raffle.RaffleId,
            Position = 1,
            Description = "Prêmio principal QA"
        };

        IFlurlResponse response;
        try
        {
            response = await _fixture.Client
                .Request("raffle-awards")
                .PostJsonAsync(dto);
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"POST /raffle-awards falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        response.StatusCode.Should().Be(201);
        var info = await response.GetJsonAsync<RaffleAwardInfo>();
        info.Should().NotBeNull();
        info!.RaffleAwardId.Should().BeGreaterThan(0);
        info.RaffleId.Should().Be(raffle.RaffleId);
        info.Position.Should().Be(dto.Position);
        info.Description.Should().Be(dto.Description);

        var list = await _fixture.Client
            .Request("raffle-awards")
            .SetQueryParam("raffleId", raffle.RaffleId)
            .GetJsonAsync<List<RaffleAwardInfo>>();

        list.Should().Contain(a => a.RaffleAwardId == info.RaffleAwardId,
            "o award recém-criado deve aparecer na listagem do Raffle.");
    }

    private async Task<LotteryInfo> CreateDraftLotteryAsync()
    {
        var dto = new LotteryInsertInfo
        {
            StoreId = _fixture.StoreId,
            Name = UniqueId.New("qa-lottery-award"),
            DescriptionMd = "# Descrição QA",
            RulesMd = "## Regras QA",
            PrivacyPolicyMd = "## Privacidade QA",
            TicketPrice = 10m,
            TotalPrizeValue = 1000m,
            TicketMin = 1,
            TicketMax = 0,
            TicketNumIni = 1,
            TicketNumEnd = 1000,
            NumberType = NumberTypeDto.Int64,
            NumberValueMin = 1,
            NumberValueMax = 1000,
            ReferralPercent = 0f
        };

        var response = await _fixture.Client.Request("lotteries").PostJsonAsync(dto);
        return await response.GetJsonAsync<LotteryInfo>();
    }

    private async Task<RaffleInfo> CreateRaffleAsync(long lotteryId)
    {
        var dto = new RaffleInsertInfo
        {
            LotteryId = lotteryId,
            Name = "Sorteio QA para Award",
            DescriptionMd = "## Sorteio QA",
            RaffleDatetime = DateTime.UtcNow.AddDays(30),
            IncludePreviousWinners = false
        };

        var response = await _fixture.Client.Request("raffles").PostJsonAsync(dto);
        return await response.GetJsonAsync<RaffleInfo>();
    }
}
