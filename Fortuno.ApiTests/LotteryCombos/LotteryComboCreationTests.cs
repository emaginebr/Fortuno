using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.LotteryCombo;

namespace Fortuno.ApiTests.LotteryCombos;

[Collection("api")]
public class LotteryComboCreationTests
{
    private readonly ApiSessionFixture _fixture;

    public LotteryComboCreationTests(ApiSessionFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_ShouldPersistComboForDraftLottery()
    {
        var lottery = await CreateDraftLotteryAsync();

        var dto = new LotteryComboInsertInfo
        {
            LotteryId = lottery.LotteryId,
            Name = "Combo QA 10x",
            DiscountValue = 10f,
            DiscountLabel = "10% off",
            QuantityStart = 10,
            QuantityEnd = 49
        };

        IFlurlResponse response;
        try
        {
            response = await _fixture.Client
                .Request("lottery-combos")
                .PostJsonAsync(dto);
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"POST /lottery-combos falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        response.StatusCode.Should().Be(201);
        var info = await response.GetJsonAsync<LotteryComboInfo>();
        info.Should().NotBeNull();
        info!.LotteryComboId.Should().BeGreaterThan(0);
        info.LotteryId.Should().Be(lottery.LotteryId);
        info.Name.Should().Be(dto.Name);
        info.QuantityStart.Should().Be(dto.QuantityStart);
        info.QuantityEnd.Should().Be(dto.QuantityEnd);

        var list = await _fixture.Client
            .Request("lottery-combos", "lottery", lottery.LotteryId)
            .GetJsonAsync<List<LotteryComboInfo>>();

        list.Should().Contain(c => c.LotteryComboId == info.LotteryComboId,
            "o combo recém-criado deve aparecer na listagem da Lottery.");
    }

    private async Task<LotteryInfo> CreateDraftLotteryAsync()
    {
        var dto = new LotteryInsertInfo
        {
            Name = UniqueId.New("qa-lottery-combo"),
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
