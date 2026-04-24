using FluentAssertions;
using Flurl.Http;
using Fortuno.ApiTests._Fixtures;

namespace Fortuno.ApiTests._Smoke;

[Collection("api")]
public class AuthenticationSmokeTests
{
    private readonly ApiSessionFixture _fixture;

    public AuthenticationSmokeTests(ApiSessionFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Cenário US1 #1: prova que o token emitido pelo NAuth e injetado pela fixture
    /// autoriza uma chamada autenticada contra a API (GET /lotteries/mine).
    /// </summary>
    [Fact]
    public async Task Login_ShouldObtainTokenAndAuthorizeAuthenticatedCall()
    {
        var response = await _fixture.Client
            .Request("lotteries", "mine")
            .GetAsync();

        response.StatusCode.Should().Be(200,
            "a fixture autenticou via NAuth e o token deve autorizar a chamada.");
    }
}
