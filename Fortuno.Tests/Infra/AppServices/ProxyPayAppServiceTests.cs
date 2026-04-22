using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Fortuno.DTO.ProxyPay;
using Fortuno.DTO.Settings;
using Fortuno.Infra.AppServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
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
        var httpContext = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var sut = new ProxyPayAppService(http, options, httpContext, NullLogger<ProxyPayAppService>.Instance);
        return (sut, handler);
    }

    private static HttpResponseMessage Json(object payload, HttpStatusCode status = HttpStatusCode.OK) =>
        new()
        {
            StatusCode = status,
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

    [Fact]
    public async Task GetStoreAsync_ShouldReturnStoreWithClientId_When200()
    {
        var (sut, _) = CreateSut(Json(new
        {
            data = new { myStore = new[] { new { storeId = 10L, userId = 42L, clientId = "abc123", name = "Loja" } } }
        }));

        var result = await sut.GetStoreAsync(10);

        result.Should().NotBeNull();
        result!.StoreId.Should().Be(10);
        result.OwnerUserId.Should().Be(42);
        result.ClientId.Should().Be("abc123");
    }

    [Fact]
    public async Task GetStoreAsync_ShouldReturnNull_WhenStoreNotInList()
    {
        var (sut, _) = CreateSut(Json(new { data = new { myStore = Array.Empty<object>() } }));

        var result = await sut.GetStoreAsync(10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStoreAsync_ShouldReturnNull_When404()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await sut.GetStoreAsync(10);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateQRCodeAsync_ShouldMapResponse()
    {
        var (sut, _) = CreateSut(Json(new
        {
            invoiceId = 1L,
            invoiceNumber = "INV-0001-000001",
            brCode = "00020101...",
            brCodeBase64 = "data:image/png;base64,iVBOR...",
            expiredAt = "2026-04-19T14:20:00.348+00:00"
        }, HttpStatusCode.Created));

        var result = await sut.CreateQRCodeAsync(new ProxyPayQRCodeRequest
        {
            ClientId = "abc123",
            Customer = new ProxyPayCustomer
            {
                Name = "John", Email = "j@e.com", DocumentId = "89639766100", Cellphone = "11999999999"
            },
            Items = new List<ProxyPayItem>
            {
                new() { Id = "LOTTERY-1", Description = "Test", Quantity = 1, UnitPrice = 10m, Discount = 0 }
            }
        });

        result.Should().NotBeNull();
        result.InvoiceId.Should().Be(1);
        result.InvoiceNumber.Should().Be("INV-0001-000001");
        result.BrCode.Should().StartWith("00020101");
        result.BrCodeBase64.Should().StartWith("data:image/png;base64,");
    }

    [Fact]
    public async Task CreateQRCodeAsync_ShouldThrow_When4xx()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"invalid\"}", Encoding.UTF8, "application/json")
        });

        Func<Task> act = () => sut.CreateQRCodeAsync(new ProxyPayQRCodeRequest { ClientId = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*status 400*");
    }

    [Fact]
    public async Task CreateQRCodeAsync_ShouldThrow_When5xx()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Func<Task> act = () => sut.CreateQRCodeAsync(new ProxyPayQRCodeRequest { ClientId = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetQRCodeStatusAsync_ShouldReturnNumericStatus(int statusInt)
    {
        var (sut, _) = CreateSut(Json(new { invoiceId = 1L, status = statusInt }));

        var result = await sut.GetQRCodeStatusAsync(1);

        result.Should().NotBeNull();
        result!.Status.Should().Be(statusInt);
    }

    [Fact]
    public async Task GetQRCodeStatusAsync_ShouldReturnPaidWithTimestamp()
    {
        var (sut, _) = CreateSut(Json(new
        {
            invoiceId = 1L,
            status = 1,
            paidAt = "2026-04-19T14:25:00+00:00"
        }));

        var result = await sut.GetQRCodeStatusAsync(1);

        result.Should().NotBeNull();
        result!.Status.Should().Be(1);
        result.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetQRCodeStatusAsync_ShouldReturnNull_When404()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.NotFound));

        var result = await sut.GetQRCodeStatusAsync(1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetQRCodeStatusAsync_ShouldReturnNull_When5xx()
    {
        var (sut, _) = CreateSut(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await sut.GetQRCodeStatusAsync(1);

        result.Should().BeNull();
    }
}
