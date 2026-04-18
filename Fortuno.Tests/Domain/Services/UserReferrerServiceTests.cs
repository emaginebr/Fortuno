using FluentAssertions;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class UserReferrerServiceTests
{
    private readonly Mock<IUserReferrerRepository<UserReferrer>> _repo = new();

    private UserReferrerService CreateSut() => new(_repo.Object);

    [Fact]
    public async Task GetOrCreateCodeForUserAsync_ShouldReturnExisting_WhenFound()
    {
        _repo.Setup(r => r.GetByUserIdAsync(42))
            .ReturnsAsync(new UserReferrer { UserId = 42, ReferralCode = "ABC12345" });

        var sut = CreateSut();
        var code = await sut.GetOrCreateCodeForUserAsync(42);

        code.Should().Be("ABC12345");
        _repo.Verify(r => r.InsertAsync(It.IsAny<UserReferrer>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateCodeForUserAsync_ShouldGenerateAndInsert_WhenMissing()
    {
        _repo.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((UserReferrer?)null);
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repo.Setup(r => r.InsertAsync(It.IsAny<UserReferrer>()))
            .ReturnsAsync((UserReferrer u) => u);

        var sut = CreateSut();
        var code = await sut.GetOrCreateCodeForUserAsync(42);

        code.Should().HaveLength(8);
        code.Should().MatchRegex("^[A-Z2-9]+$");
    }

    [Fact]
    public async Task GetOrCreateCodeForUserAsync_ShouldRetry_OnCodeCollision()
    {
        _repo.Setup(r => r.GetByUserIdAsync(42)).ReturnsAsync((UserReferrer?)null);
        _repo.SetupSequence(r => r.CodeExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        _repo.Setup(r => r.InsertAsync(It.IsAny<UserReferrer>()))
            .ReturnsAsync((UserReferrer u) => u);

        var sut = CreateSut();
        var code = await sut.GetOrCreateCodeForUserAsync(42);

        code.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetOrCreateCodeForUserAsync_ShouldFallback_WhenInsertThrows()
    {
        _repo.SetupSequence(r => r.GetByUserIdAsync(42))
            .ReturnsAsync((UserReferrer?)null)
            .ReturnsAsync(new UserReferrer { UserId = 42, ReferralCode = "FALLBCK1" });
        _repo.Setup(r => r.CodeExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        _repo.Setup(r => r.InsertAsync(It.IsAny<UserReferrer>()))
            .ThrowsAsync(new Exception("unique constraint"));

        var sut = CreateSut();
        var code = await sut.GetOrCreateCodeForUserAsync(42);

        code.Should().Be("FALLBCK1");
    }

    [Fact]
    public async Task GetCodeForUserAsync_ShouldReturnCodeOrNull()
    {
        _repo.Setup(r => r.GetByUserIdAsync(42))
            .ReturnsAsync(new UserReferrer { UserId = 42, ReferralCode = "ABCD1234" });
        _repo.Setup(r => r.GetByUserIdAsync(99)).ReturnsAsync((UserReferrer?)null);

        var sut = CreateSut();
        (await sut.GetCodeForUserAsync(42)).Should().Be("ABCD1234");
        (await sut.GetCodeForUserAsync(99)).Should().BeNull();
    }

    [Fact]
    public async Task ResolveReferrerUserIdAsync_ShouldReturnNull_WhenInputBlank()
    {
        var sut = CreateSut();
        (await sut.ResolveReferrerUserIdAsync(null)).Should().BeNull();
        (await sut.ResolveReferrerUserIdAsync("")).Should().BeNull();
        (await sut.ResolveReferrerUserIdAsync("   ")).Should().BeNull();
    }

    [Fact]
    public async Task ResolveReferrerUserIdAsync_ShouldNormalizeInput()
    {
        _repo.Setup(r => r.GetByCodeAsync("ABC12345"))
            .ReturnsAsync(new UserReferrer { UserId = 7, ReferralCode = "ABC12345" });

        var sut = CreateSut();
        var result = await sut.ResolveReferrerUserIdAsync(" abc12345 ");

        result.Should().Be(7);
    }

    [Fact]
    public async Task ResolveReferrerUserIdAsync_ShouldReturnNull_WhenCodeNotFound()
    {
        _repo.Setup(r => r.GetByCodeAsync(It.IsAny<string>())).ReturnsAsync((UserReferrer?)null);

        var sut = CreateSut();
        (await sut.ResolveReferrerUserIdAsync("XXX99999")).Should().BeNull();
    }
}
