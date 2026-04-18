using FluentAssertions;
using Fortuno.Domain.Services;
using Fortuno.Infra.Interfaces.AppServices;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class StoreOwnershipGuardTests
{
    private readonly Mock<IProxyPayAppService> _proxyPay = new();

    private StoreOwnershipGuard CreateSut() => new(_proxyPay.Object);

    [Fact]
    public async Task IsOwnerAsync_ShouldReturnTrue_WhenOwnerMatches()
    {
        _proxyPay.Setup(p => p.GetStoreAsync(10))
            .ReturnsAsync(new ProxyPayStoreInfo { StoreId = 10, OwnerUserId = 42, Name = "Loja" });

        var sut = CreateSut();
        var result = await sut.IsOwnerAsync(10, 42);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsOwnerAsync_ShouldReturnFalse_WhenOwnerDiffers()
    {
        _proxyPay.Setup(p => p.GetStoreAsync(10))
            .ReturnsAsync(new ProxyPayStoreInfo { StoreId = 10, OwnerUserId = 99 });

        var sut = CreateSut();
        var result = await sut.IsOwnerAsync(10, 42);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsOwnerAsync_ShouldReturnFalse_WhenStoreNotFound()
    {
        _proxyPay.Setup(p => p.GetStoreAsync(It.IsAny<long>()))
            .ReturnsAsync((ProxyPayStoreInfo?)null);

        var sut = CreateSut();
        var result = await sut.IsOwnerAsync(10, 42);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureOwnershipAsync_ShouldThrow_WhenNotOwner()
    {
        _proxyPay.Setup(p => p.GetStoreAsync(10))
            .ReturnsAsync(new ProxyPayStoreInfo { StoreId = 10, OwnerUserId = 99 });

        var sut = CreateSut();
        Func<Task> act = () => sut.EnsureOwnershipAsync(10, 42);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task EnsureOwnershipAsync_ShouldNotThrow_WhenOwner()
    {
        _proxyPay.Setup(p => p.GetStoreAsync(10))
            .ReturnsAsync(new ProxyPayStoreInfo { StoreId = 10, OwnerUserId = 42 });

        var sut = CreateSut();
        Func<Task> act = () => sut.EnsureOwnershipAsync(10, 42);

        await act.Should().NotThrowAsync();
    }
}
