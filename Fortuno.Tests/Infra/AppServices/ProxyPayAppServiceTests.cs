using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Fortuno.DTO.Settings;
using Fortuno.Infra.AppServices;
using Fortuno.Infra.Interfaces.AppServices;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Fortuno.Tests.Infra.AppServices;

public class ProxyPayAppServiceTests
{
    private static (ProxyPayAppService sut, Mock<HttpMessageHandler> handler) CreateSut(
        HttpResponseMessage response)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var http = new HttpClient(handler.Object);
        var options = Options.Create(new FortunoSettings
        {
            ProxyPay = new ProxyPaySettings
            {
                ApiUrl = "https://proxypay.test",
                TenantId = "fortuna"
            }
        });
        var sut = new ProxyPayAppService(http, options);
        return (sut, handler);
    }

    private static HttpResponseMessage Json(object payload, HttpStatusCode status = HttpStatusCode.OK) =>
        new()
        {
            StatusCode = status,
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task GetStoreAsync_ShouldReturnStore_When200()
    {
        var (sut, _) = CreateSut(Json(new { storeId = 10L, ownerUserId = 42L, name = "Loja" }));

        var result = await sut.GetStoreAsync(10);

        result.Should().NotBeNull();
        result!.StoreId.Should().Be(10);
        result.OwnerUserId.Should().Be(42);
    }

    [Fact]
    public async Task GetStoreAsync_ShouldReturnNull_When404()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await sut.GetStoreAsync(10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldMapResponse()
    {
        var (sut, _) = CreateSut(Json(new
        {
            invoiceId = 123L,
            storeId = 10L,
            amount = 50m,
            status = "pending",
            pixCopyPaste = "pix-code",
            pixQrCode = "qr"
        }));

        var result = await sut.CreateInvoiceAsync(new ProxyPayCreateInvoiceRequest
        {
            StoreId = 10,
            Amount = 50m,
            Description = "test"
        });

        result.InvoiceId.Should().Be(123);
        result.PixCopyPaste.Should().Be("pix-code");
        result.Status.Should().Be("pending");
    }

    [Fact]
    public async Task CreateInvoiceAsync_ShouldThrow_WhenStatusNotSuccess()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Func<Task> act = () => sut.CreateInvoiceAsync(new ProxyPayCreateInvoiceRequest { StoreId = 10 });

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetInvoiceAsync_ShouldReturnNull_When404()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await sut.GetInvoiceAsync(123);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInvoiceAsync_ShouldMapFields_When200()
    {
        var (sut, _) = CreateSut(Json(new
        {
            invoiceId = 9L,
            storeId = 10L,
            amount = 100m,
            paidAmount = 100m,
            status = "paid"
        }));

        var result = await sut.GetInvoiceAsync(9);

        result.Should().NotBeNull();
        result!.PaidAmount.Should().Be(100m);
        result.Status.Should().Be("paid");
    }
}
