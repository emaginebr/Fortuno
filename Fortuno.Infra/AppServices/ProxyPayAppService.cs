using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fortuno.DTO.ProxyPay;
using Fortuno.DTO.Settings;
using Fortuno.Infra.Interfaces.AppServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fortuno.Infra.AppServices;

public class ProxyPayAppService : IProxyPayAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
            Content = JsonContent.Create(new { query = "{ myStore { storeId userId clientId name } }" })
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

        var payload = JsonSerializer.Deserialize<GraphQLResponse<MyStoreData>>(body, JsonOptions);
        var store = payload?.Data?.MyStore?.FirstOrDefault(s => s.StoreId == storeId);
        if (store is null)
        {
            _logger.LogInformation(
                "ProxyPay: storeId={StoreId} não encontrado em myStore.", storeId);
            return null;
        }

        _logger.LogInformation(
            "ProxyPay: store encontrada storeId={StoreId} userId={UserId} clientId={ClientId}",
            store.StoreId, store.UserId, store.ClientId);

        return new ProxyPayStoreInfo
        {
            StoreId = store.StoreId,
            OwnerUserId = store.UserId,
            Name = store.Name ?? string.Empty,
            ClientId = store.ClientId ?? string.Empty
        };
    }

    public async Task<ProxyPayStoreInfo?> GetMyStoreAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "graphql")
        {
            Content = JsonContent.Create(new { query = "{ myStore { storeId userId clientId name } }" })
        };
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", _settings.TenantId);
        ForwardAuthHeader(req);

        _logger.LogInformation(
            "ProxyPay: POST {Url}/graphql (tenant={Tenant}) — query myStore para resolver store do usuário autenticado",
            _settings.ApiUrl, _settings.TenantId);

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta myStore status={Status} body={Body}",
            (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode) return null;

        var payload = JsonSerializer.Deserialize<GraphQLResponse<MyStoreData>>(body, JsonOptions);
        var store = payload?.Data?.MyStore?.FirstOrDefault();
        if (store is null)
        {
            _logger.LogInformation("ProxyPay: myStore vazio — usuário autenticado não possui Store.");
            return null;
        }

        return new ProxyPayStoreInfo
        {
            StoreId = store.StoreId,
            OwnerUserId = store.UserId,
            Name = store.Name ?? string.Empty,
            ClientId = store.ClientId ?? string.Empty
        };
    }

    public async Task<ProxyPayStoreInfo> CreateStoreAsync(string name, string email)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "store")
        {
            Content = JsonContent.Create(new { name, email, billingStrategy = 1 })
        };
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", _settings.TenantId);
        ForwardAuthHeader(req);

        _logger.LogInformation(
            "ProxyPay: POST {Url}/store (tenant={Tenant}) — criando Store name={Name} email={Email}",
            _settings.ApiUrl, _settings.TenantId, name, email);

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta CreateStore status={Status} body={Body}",
            (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"ProxyPay indisponível ao criar Store (status {(int)res.StatusCode}). Body: {Truncate(body, 500)}");

        var created = JsonSerializer.Deserialize<StoreDto>(body, JsonOptions)
            ?? throw new InvalidOperationException("ProxyPay retornou resposta vazia ao criar Store.");
        if (created.StoreId <= 0)
            throw new InvalidOperationException($"ProxyPay retornou storeId inválido ao criar Store. Body: {Truncate(body, 500)}");

        return new ProxyPayStoreInfo
        {
            StoreId = created.StoreId,
            OwnerUserId = created.UserId,
            Name = created.Name ?? name,
            ClientId = created.ClientId ?? string.Empty
        };
    }

    public async Task<ProxyPayStoreInfo> EnsureMyStoreAsync(string name, string email)
    {
        var existing = await GetMyStoreAsync();
        if (existing is not null) return existing;

        _logger.LogInformation(
            "ProxyPay: usuário sem Store; criando automaticamente via POST /store (name={Name})", name);
        return await CreateStoreAsync(name, email);
    }

    public async Task<ProxyPayQRCodeResponse> CreateQRCodeAsync(ProxyPayQRCodeRequest request)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "payment/qrcode")
        {
            Content = JsonContent.Create(request)
        };
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", _settings.TenantId);
        ForwardAuthHeader(req);

        _logger.LogInformation(
            "ProxyPay: POST /payment/qrcode clientId={ClientId} quantity={Qty}",
            request.ClientId, request.Items.Sum(i => i.Quantity));

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta CreateQRCode status={Status} body={Body}",
            (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"ProxyPay indisponível ao criar QR Code (status {(int)res.StatusCode}). Body: {Truncate(body, 500)}");

        var dto = JsonSerializer.Deserialize<ProxyPayQRCodeResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException("ProxyPay retornou resposta vazia ao criar QR Code.");
        return dto;
    }

    public async Task<ProxyPayQRCodeStatusResponse?> GetQRCodeStatusAsync(long invoiceId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"payment/qrcode/status/{invoiceId}");
        req.Headers.TryAddWithoutValidation("X-Tenant-Id", _settings.TenantId);
        ForwardAuthHeader(req);

        _logger.LogInformation("ProxyPay: GET /payment/qrcode/status/{InvoiceId}", invoiceId);

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();
        _logger.LogInformation(
            "ProxyPay: resposta QRCodeStatus status={Status} body={Body}",
            (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode) return null;

        return JsonSerializer.Deserialize<ProxyPayQRCodeStatusResponse>(body, JsonOptions);
    }

    private void ForwardAuthHeader(HttpRequestMessage req)
    {
        var auth = _httpContext.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(auth))
            req.Headers.TryAddWithoutValidation("Authorization", auth);
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "<vazio>" : (s.Length <= max ? s : s[..max] + "...");

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
        [JsonPropertyName("clientId")] public string? ClientId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
