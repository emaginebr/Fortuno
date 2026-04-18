using FluentAssertions;
using Fortuno.Infra.AppServices;
using Microsoft.AspNetCore.Http;
using Moq;
using NAuth.ACL.Interfaces;
using NAuth.DTO.User;

namespace Fortuno.Tests.Infra.AppServices;

public class NAuthAppServiceTests
{
    private readonly Mock<IUserClient> _userClient = new();
    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IHttpContextAccessor> _accessor = new();

    public NAuthAppServiceTests()
    {
        _accessor.Setup(a => a.HttpContext).Returns(_httpContext);
    }

    private NAuthAppService CreateSut() => new(_userClient.Object, _accessor.Object);

    private void SetAuthHeader(string value) =>
        _httpContext.Request.Headers.Authorization = value;

    [Fact]
    public async Task GetByIdAsync_ShouldMapUserInfo()
    {
        SetAuthHeader("Basic token-abc");
        _userClient.Setup(c => c.GetByIdAsync(42, "token-abc"))
            .ReturnsAsync(new UserInfo
            {
                UserId = 42,
                Name = "Fulano",
                Email = "fulano@x.com",
                IdDocument = "12345678900"
            });

        var sut = CreateSut();
        var result = await sut.GetByIdAsync(42);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(42);
        result.Name.Should().Be("Fulano");
        result.DocumentId.Should().Be("12345678900");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserClientReturnsNull()
    {
        SetAuthHeader("Bearer tk");
        _userClient.Setup(c => c.GetByIdAsync(It.IsAny<long>(), It.IsAny<string>()))
            .ReturnsAsync((UserInfo?)null);

        var sut = CreateSut();
        (await sut.GetByIdAsync(42)).Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldReturnNull_WhenNoAuthHeader()
    {
        // nenhum header
        var sut = CreateSut();
        var result = await sut.GetCurrentAsync();

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_ShouldCallGetMeAsyncWithExtractedToken()
    {
        SetAuthHeader("Bearer tk-xyz");
        _userClient.Setup(c => c.GetMeAsync("tk-xyz"))
            .ReturnsAsync(new UserInfo { UserId = 7, Name = "Me", Email = "me@x" });

        var sut = CreateSut();
        var result = await sut.GetCurrentAsync();

        result.Should().NotBeNull();
        result!.UserId.Should().Be(7);
        _userClient.Verify(c => c.GetMeAsync("tk-xyz"), Times.Once);
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldDeduplicateAndMap()
    {
        SetAuthHeader("Basic xyz");
        _userClient.Setup(c => c.GetByIdAsync(1, "xyz"))
            .ReturnsAsync(new UserInfo { UserId = 1, Name = "A", Email = "a@x" });
        _userClient.Setup(c => c.GetByIdAsync(2, "xyz"))
            .ReturnsAsync(new UserInfo { UserId = 2, Name = "B", Email = "b@x" });

        var sut = CreateSut();
        var result = await sut.GetByIdsAsync(new[] { 1L, 2L, 1L });

        result.Should().HaveCount(2);
        _userClient.Verify(c => c.GetByIdAsync(1, "xyz"), Times.Once);
        _userClient.Verify(c => c.GetByIdAsync(2, "xyz"), Times.Once);
    }

    [Fact]
    public async Task GetByIdsAsync_ShouldSkipMissingUsers()
    {
        SetAuthHeader("Basic token");
        _userClient.Setup(c => c.GetByIdAsync(1, "token"))
            .ReturnsAsync(new UserInfo { UserId = 1, Name = "A", Email = "a@x" });
        _userClient.Setup(c => c.GetByIdAsync(2, "token"))
            .ReturnsAsync((UserInfo?)null);

        var sut = CreateSut();
        var result = await sut.GetByIdsAsync(new[] { 1L, 2L });

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(1);
    }
}
