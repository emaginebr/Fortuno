using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.LotteryImage;
using Fortuno.DTO.Raffle;
using Fortuno.DTO.RaffleAward;

namespace Fortuno.ApiTests.Lotteries;

[Collection("api")]
public class LotteryActivationTests
{
    // PNG 1x1 transparente.
    private const string Tiny1x1Png =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    private readonly ApiSessionFixture _fixture;

    public LotteryActivationTests(ApiSessionFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Publish_AfterAllPrerequisites_ShouldTransitionToOpen()
    {
        // 1) Cria Lottery em Draft
        var lottery = await PostAsync<LotteryInsertInfo, LotteryInfo>("lotteries", BuildLottery());
        lottery.Status.Should().Be(LotteryStatusDto.Draft);

        // 2) Cadastra pré-requisitos: imagem, raffle, award
        var image = await PostAsync<LotteryImageInsertInfo, LotteryImageInfo>("lottery-images", new()
        {
            LotteryId = lottery.LotteryId,
            ImageBase64 = Tiny1x1Png,
            Description = "QA activation image",
            DisplayOrder = 0
        });
        image.LotteryImageId.Should().BeGreaterThan(0);

        var raffle = await PostAsync<RaffleInsertInfo, RaffleInfo>("raffles", new()
        {
            LotteryId = lottery.LotteryId,
            Name = "Sorteio QA activation",
            DescriptionMd = "## Sorteio QA",
            RaffleDatetime = DateTime.UtcNow.AddDays(30),
            IncludePreviousWinners = false
        });
        raffle.RaffleId.Should().BeGreaterThan(0);

        var award = await PostAsync<RaffleAwardInsertInfo, RaffleAwardInfo>("raffle-awards", new()
        {
            RaffleId = raffle.RaffleId,
            Position = 1,
            Description = "Prêmio principal QA activation"
        });
        award.RaffleAwardId.Should().BeGreaterThan(0);

        // 3) Publica — deve transitar Draft → Open
        IFlurlResponse publishResponse;
        try
        {
            publishResponse = await _fixture.Client
                .Request("lotteries", lottery.LotteryId, "publish")
                .PostAsync();
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem body>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"POST /lotteries/{lottery.LotteryId}/publish falhou com {ex.StatusCode}. Body: {body}", ex);
        }

        publishResponse.StatusCode.Should().Be(200,
            "com imagem, raffle e award cadastrados, a Lottery deve ser ativada sem erros.");

        // 4) Confirma estado final
        var after = await _fixture.Client
            .Request("lotteries", lottery.LotteryId)
            .GetJsonAsync<LotteryInfo>();
        after.Status.Should().Be(LotteryStatusDto.Open);
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
            throw new InvalidOperationException(
                $"POST /{path} falhou com {ex.StatusCode}. Body: {errBody}", ex);
        }
    }

    private LotteryInsertInfo BuildLottery() => new()
    {
        StoreId = _fixture.StoreId,
        Name = UniqueId.New("qa-lottery-activation"),
        DescriptionMd = "# Descrição QA activation",
        RulesMd = "## Regras QA activation",
        PrivacyPolicyMd = "## Privacidade QA activation",
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
}
