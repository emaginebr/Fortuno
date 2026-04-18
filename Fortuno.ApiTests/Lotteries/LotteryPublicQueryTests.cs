using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;

namespace Fortuno.ApiTests.Lotteries;

[Collection("api")]
public class LotteryPublicQueryTests
{
    private readonly ApiSessionFixture _fixture;

    public LotteryPublicQueryTests(ApiSessionFixture fixture)
    {
        _fixture = fixture;
    }

    // ---------- Cenário US1 #7 — GET /api/lotteries/{id} é [AllowAnonymous] ----------
    [Fact]
    public async Task GetById_WithoutAuth_ShouldReturn200()
    {
        var created = await CreateDraftAsync();

        var baseUrl = _fixture.Client.BaseUrl.TrimEnd('/');
        var anonymousResponse = await new FlurlRequest($"{baseUrl}/api/lotteries/{created.LotteryId}")
            .AllowHttpStatus("2xx,4xx")
            .GetAsync();

        anonymousResponse.StatusCode.Should().Be(200,
            "GET /api/lotteries/{id} é [AllowAnonymous] e deve responder sem Authorization header.");

        var info = await anonymousResponse.GetJsonAsync<LotteryInfo>();
        info.LotteryId.Should().Be(created.LotteryId);
    }

    [Fact]
    public async Task GetBySlug_WithoutAuth_ShouldReturn200()
    {
        var created = await CreateDraftAsync();
        created.Slug.Should().NotBeNullOrWhiteSpace();

        var baseUrl = _fixture.Client.BaseUrl.TrimEnd('/');
        var anonymousResponse = await new FlurlRequest($"{baseUrl}/api/lotteries/slug/{created.Slug}")
            .AllowHttpStatus("2xx,4xx")
            .GetAsync();

        anonymousResponse.StatusCode.Should().Be(200,
            "GET /api/lotteries/slug/{slug} é [AllowAnonymous] e deve responder sem Authorization header.");

        var info = await anonymousResponse.GetJsonAsync<LotteryInfo>();
        info.Slug.Should().Be(created.Slug);
    }

    // ---------- helpers ----------

    private async Task<LotteryInfo> CreateDraftAsync()
    {
        var dto = new LotteryInsertInfo
        {
            StoreId = _fixture.StoreId,
            Name = UniqueId.New("qa-lottery"),
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

        var response = await _fixture.Client
            .Request("api", "lotteries")
            .PostJsonAsync(dto);
        return await response.GetJsonAsync<LotteryInfo>();
    }
}
