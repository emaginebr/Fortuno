using FluentAssertions;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class SlugServiceTests
{
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<IZToolsAppService> _zTools = new();

    private SlugService CreateSut() => new(_lotteryRepo.Object, _zTools.Object);

    [Fact]
    public async Task GenerateUniqueSlugAsync_ShouldReturnBase_WhenNotTaken()
    {
        _zTools.Setup(z => z.GenerateSlugAsync("Rifa QA")).ReturnsAsync("rifa-qa");
        _lotteryRepo.Setup(r => r.SlugExistsAsync("rifa-qa")).ReturnsAsync(false);

        var sut = CreateSut();
        var slug = await sut.GenerateUniqueSlugAsync("Rifa QA");

        slug.Should().Be("rifa-qa");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ShouldAppendSuffix_OnCollision()
    {
        _zTools.Setup(z => z.GenerateSlugAsync(It.IsAny<string>())).ReturnsAsync("rifa-qa");
        _lotteryRepo.SetupSequence(r => r.SlugExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true)   // rifa-qa
            .ReturnsAsync(true)   // rifa-qa-2
            .ReturnsAsync(false); // rifa-qa-3

        var sut = CreateSut();
        var slug = await sut.GenerateUniqueSlugAsync("Rifa QA");

        slug.Should().Be("rifa-qa-3");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ShouldFallback_WhenZToolsThrows()
    {
        _zTools.Setup(z => z.GenerateSlugAsync(It.IsAny<string>())).ThrowsAsync(new Exception("offline"));
        _lotteryRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        var sut = CreateSut();
        var slug = await sut.GenerateUniqueSlugAsync("Ação Café");

        // Fallback local remove acentos e substitui espaços por hífen
        slug.Should().Be("acao-cafe");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ShouldFallback_WhenZToolsReturnsEmpty()
    {
        _zTools.Setup(z => z.GenerateSlugAsync(It.IsAny<string>())).ReturnsAsync(string.Empty);
        _lotteryRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        var sut = CreateSut();
        var slug = await sut.GenerateUniqueSlugAsync("Sorteio Especial");

        slug.Should().Be("sorteio-especial");
    }

    [Fact]
    public async Task GenerateUniqueSlugAsync_ShouldDefaultToLottery_WhenInputEmpty()
    {
        _zTools.Setup(z => z.GenerateSlugAsync(It.IsAny<string>())).ReturnsAsync(string.Empty);
        _lotteryRepo.Setup(r => r.SlugExistsAsync(It.IsAny<string>())).ReturnsAsync(false);

        var sut = CreateSut();
        var slug = await sut.GenerateUniqueSlugAsync("");

        slug.Should().Be("lottery");
    }
}
