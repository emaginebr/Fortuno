using Fortuno.DTO.ProxyPay;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Fortuno.DTO.Settings;
using Fortuno.Infra.Interfaces.AppServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fortuno.Infra.AppServices;

public class ProxyPayAppService : IProxyPayAppService
{
    private readonly HttpClient _http;
    private readonly ProxyPaySettings _settings;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ILogger<ProxyPayAppService> _logger;

    public ProxyPayAppService(
        HttpClient http,
        IOptions<FortunoSettings> options,
        IHttpContextAccessor httpContext,
        ILogger<ProxyPayAppService> logger)
    {
        _http = http;
        _settings = options.Value.ProxyPay;
        _httpContext = httpContext;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_settings.ApiUrl))
        {
            // BaseAddress precisa terminar com `/` para que paths relativos
            // (ex.: `graphql`) preservem o segmento `/api` da configuração.
            var baseUrl = _settings.ApiUrl.EndsWith('/') ? _settings.ApiUrl : _settings.ApiUrl + "/";
            _http.BaseAddress = new Uri(baseUrl);
        }

        if (!_http.DefaultRequestHeaders.Contains("X-Tenant-Id"))
            _http.DefaultRequestHeaders.Add("X-Tenant-Id", _settings.TenantId);
    }

    public async Task<ProxyPayStoreInfo?> GetStoreAsync(long storeId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "graphql")
        {
            Content = JsonContent.Create(new { query = "{ myStore { storeId userId name } }" })
        };
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", _settings.TenantId);
        ForwardAuthHeader(req);

        _logger.LogInformation(
            "ProxyPay: POST {Url}/graphql (tenant={Tenant}) — query myStore para resolver storeId={StoreId}",
            _settings.ApiUrl, _settings.TenantId, storeId);

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta myStore status={Status} body={Body}",
            (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode) return null;

        var payload = System.Text.Json.JsonSerializer.Deserialize<GraphQLResponse<MyStoreData>>(body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var store = payload?.Data?.MyStore?.FirstOrDefault(s => s.StoreId == storeId);
        if (store is null)
        {
            _logger.LogInformation(
                "ProxyPay: storeId={StoreId} não encontrado na lista de myStore do usuário autenticado.",
                storeId);
            return null;
        }

        _logger.LogInformation(
            "ProxyPay: store encontrada storeId={StoreId} userId={UserId} name={Name}",
            store.StoreId, store.UserId, store.Name);

        return new ProxyPayStoreInfo
        {
            StoreId = store.StoreId,
            OwnerUserId = store.UserId,
            Name = store.Name ?? string.Empty
        };
    }

    public async Task<ProxyPayInvoiceInfo> CreateInvoiceAsync(ProxyPayCreateInvoiceRequest request)
    {
        _logger.LogInformation(
            "ProxyPay: POST /api/invoices storeId={StoreId} amount={Amount}",
            request.StoreId, request.Amount);

        var res = await _http.PostAsJsonAsync("/api/invoices", new
        {
            storeId = request.StoreId,
            amount = request.Amount,
            description = request.Description,
            metadata = request.Metadata
        });
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta CreateInvoice status={Status} body={Body}",
            (int)res.StatusCode, body);

        res.EnsureSuccessStatusCode();
        var dto = System.Text.Json.JsonSerializer.Deserialize<InvoiceDto>(body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("ProxyPay retornou resposta vazia ao criar Invoice.");
        return MapInvoice(dto);
    }

    public async Task<ProxyPayInvoiceInfo?> GetInvoiceAsync(long invoiceId)
    {
        _logger.LogInformation("ProxyPay: GET /api/invoices/{InvoiceId}", invoiceId);

        var res = await _http.GetAsync($"/api/invoices/{invoiceId}");
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta GetInvoice status={Status} body={Body}",
            (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode) return null;
        var dto = System.Text.Json.JsonSerializer.Deserialize<InvoiceDto>(body,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return dto is null ? null : MapInvoice(dto);
    }

    private void ForwardAuthHeader(HttpRequestMessage req)
    {
        var auth = _httpContext.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(auth))
            req.Headers.TryAddWithoutValidation("Authorization", auth);
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

    private class GraphQLResponse<T>
    {
        [JsonPropertyName("data")] public T? Data { get; set; }
    }

    private class MyStoreData
    {
        [JsonPropertyName("myStore")] public List<StoreDto>? MyStore { get; set; }
    }

    private class StoreDto
    {
        [JsonPropertyName("storeId")] public long StoreId { get; set; }
        [JsonPropertyName("userId")] public long UserId { get; set; }
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
