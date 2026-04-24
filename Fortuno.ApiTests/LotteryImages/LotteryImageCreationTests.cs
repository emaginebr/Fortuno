using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.LotteryImage;

namespace Fortuno.ApiTests.LotteryImages;

[Collection("api")]
public class LotteryImageCreationTests
{
    // PNG 1x1 transparente (menor payload válido aceito por upload de imagem).
    private const string Tiny1x1Png =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    private readonly ApiSessionFixture _fixture;

    public LotteryImageCreationTests(ApiSessionFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_ShouldPersistImageForDraftLottery()
    {
        var lottery = await CreateDraftLotteryAsync();

        var dto = new LotteryImageInsertInfo
        {
            LotteryId = lottery.LotteryId,
            ImageBase64 = Tiny1x1Png,
            Description = "QA smoke image",
            DisplayOrder = 0
        };

        IFlurlResponse response;
        try
        {
            response = await _fixture.Client
                .Request("lottery-images")
                .PostJsonAsync(dto);
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"POST /lottery-images falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        response.StatusCode.Should().Be(201);
        var info = await response.GetJsonAsync<LotteryImageInfo>();
        info.Should().NotBeNull();
        info!.LotteryImageId.Should().BeGreaterThan(0);
        info.LotteryId.Should().Be(lottery.LotteryId);
        info.ImageUrl.Should().NotBeNullOrEmpty();

        var list = await _fixture.Client
            .Request("lottery-images", "lottery", lottery.LotteryId)
            .GetJsonAsync<List<LotteryImageInfo>>();

        list.Should().Contain(i => i.LotteryImageId == info.LotteryImageId,
            "a imagem recém-criada deve aparecer na listagem da Lottery.");
    }

    private async Task<LotteryInfo> CreateDraftLotteryAsync()
    {
        var dto = new LotteryInsertInfo
        {
            Name = UniqueId.New("qa-lottery-img"),
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
