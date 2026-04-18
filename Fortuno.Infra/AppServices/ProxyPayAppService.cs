using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Fortuno.DTO.Settings;
using Fortuno.Infra.Interfaces.AppServices;
using Microsoft.Extensions.Options;

namespace Fortuno.Infra.AppServices;

public class ProxyPayAppService : IProxyPayAppService
{
    private readonly HttpClient _http;
    private readonly ProxyPaySettings _settings;

    public ProxyPayAppService(HttpClient http, IOptions<FortunoSettings> options)
    {
        _http = http;
        _settings = options.Value.ProxyPay;

        if (!string.IsNullOrWhiteSpace(_settings.ApiUrl))
            _http.BaseAddress = new Uri(_settings.ApiUrl);

        if (!_http.DefaultRequestHeaders.Contains("X-Tenant-Id"))
            _http.DefaultRequestHeaders.Add("X-Tenant-Id", _settings.TenantId);
    }

    public async Task<ProxyPayStoreInfo?> GetStoreAsync(long storeId)
    {
        var res = await _http.GetAsync($"/api/stores/{storeId}");
        if (!res.IsSuccessStatusCode) return null;
        var dto = await res.Content.ReadFromJsonAsync<StoreDto>();
        if (dto is null) return null;
        return new ProxyPayStoreInfo
        {
            StoreId = dto.StoreId,
            OwnerUserId = dto.OwnerUserId,
            Name = dto.Name ?? string.Empty
        };
    }

    public async Task<ProxyPayInvoiceInfo> CreateInvoiceAsync(ProxyPayCreateInvoiceRequest request)
    {
        var res = await _http.PostAsJsonAsync("/api/invoices", new
        {
            storeId = request.StoreId,
            amount = request.Amount,
            description = request.Description,
            metadata = request.Metadata
        });
        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<InvoiceDto>()
            ?? throw new InvalidOperationException("ProxyPay retornou resposta vazia ao criar Invoice.");
        return MapInvoice(dto);
    }

    public async Task<ProxyPayInvoiceInfo?> GetInvoiceAsync(long invoiceId)
    {
        var res = await _http.GetAsync($"/api/invoices/{invoiceId}");
        if (!res.IsSuccessStatusCode) return null;
        var dto = await res.Content.ReadFromJsonAsync<InvoiceDto>();
        return dto is null ? null : MapInvoice(dto);
    }

    private static ProxyPayInvoiceInfo MapInvoice(InvoiceDto dto) => new()
    {
        InvoiceId = dto.InvoiceId,
        StoreId = dto.StoreId,
        Amount = dto.Amount,
        PaidAmount = dto.PaidAmount,
        Status = dto.Status ?? string.Empty,
        PaidAt = dto.PaidAt,
        PixQrCode = dto.PixQrCode,
        PixCopyPaste = dto.PixCopyPaste,
        ExpiresAt = dto.ExpiresAt
    };

    private class StoreDto
    {
        [JsonPropertyName("storeId")] public long StoreId { get; set; }
        [JsonPropertyName("ownerUserId")] public long OwnerUserId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private class InvoiceDto
    {
        [JsonPropertyName("invoiceId")] public long InvoiceId { get; set; }
        [JsonPropertyName("storeId")] public long StoreId { get; set; }
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("paidAmount")] public decimal? PaidAmount { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("paidAt")] public DateTime? PaidAt { get; set; }
        [JsonPropertyName("pixQrCode")] public string? PixQrCode { get; set; }
        [JsonPropertyName("pixCopyPaste")] public string? PixCopyPaste { get; set; }
        [JsonPropertyName("expiresAt")] public DateTime? ExpiresAt { get; set; }
    }
}
