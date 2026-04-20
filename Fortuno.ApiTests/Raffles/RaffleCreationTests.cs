using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.Raffle;

namespace Fortuno.ApiTests.Raffles;

[Collection("api")]
public class RaffleCreationTests
{
    private readonly ApiSessionFixture _fixture;

    public RaffleCreationTests(ApiSessionFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_ShouldPersistRaffleForDraftLottery()
    {
        var lottery = await CreateDraftLotteryAsync();

        var dto = new RaffleInsertInfo
        {
            LotteryId = lottery.LotteryId,
            Name = "Sorteio QA",
            DescriptionMd = "## Sorteio\n\nSorteio gerado pelo teste QA.",
            RaffleDatetime = DateTime.UtcNow.AddDays(30),
            VideoUrl = null,
            IncludePreviousWinners = false
        };

        IFlurlResponse response;
        try
        {
            response = await _fixture.Client
                .Request("raffles")
                .PostJsonAsync(dto);
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"POST /raffles falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        response.StatusCode.Should().Be(201);
        var info = await response.GetJsonAsync<RaffleInfo>();
        info.Should().NotBeNull();
        info!.RaffleId.Should().BeGreaterThan(0);
        info.LotteryId.Should().Be(lottery.LotteryId);
        info.Name.Should().Be(dto.Name);

        var list = await _fixture.Client
            .Request("raffles", "lottery", lottery.LotteryId)
            .GetJsonAsync<List<RaffleInfo>>();

        list.Should().Contain(r => r.RaffleId == info.RaffleId,
            "o raffle recém-criado deve aparecer na listagem da Lottery.");
    }

    private async Task<LotteryInfo> CreateDraftLotteryAsync()
    {
        var dto = new LotteryInsertInfo
        {
            StoreId = _fixture.StoreId,
            Name = UniqueId.New("qa-lottery-raffle"),
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
}
