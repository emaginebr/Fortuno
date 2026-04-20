using System.Text.Json;
using Flurl.Http;

namespace Fortuno.ApiTests._Fixtures;

/// <summary>
/// Resolve o <c>StoreId</c> do usuário autenticado consultando o GraphQL do ProxyPay.
/// Se o usuário não tiver Store associada, cria uma automaticamente via REST <c>POST /store</c>
/// e a utiliza nos testes (R-001 v2).
/// </summary>
internal static class ProxyPayStoreResolver
{
    private const string Query = "{ myStore { storeId } }";

    public static async Task<long> ResolveAsync(
        string proxyPayUrl,
        string nauthToken,
        string tenant,
        string email)
    {
        var baseUrl = proxyPayUrl.TrimEnd('/');

        var existing = await QueryMyStoreAsync(baseUrl, nauthToken, tenant);
        if (existing is long id) return id;

        return await CreateStoreAsync(baseUrl, nauthToken, tenant, email);
    }

    private static async Task<long?> QueryMyStoreAsync(string baseUrl, string nauthToken, string tenant)
    {
        var endpoint = $"{baseUrl}/graphql";
        string rawBody;
        try
        {
            rawBody = await new FlurlRequest(endpoint)
                .WithHeader("Authorization", $"Basic {nauthToken}")
                .WithHeader("X-Tenant-Id", tenant)
                .PostJsonAsync(new { query = Query })
                .ReceiveString();
        }
        catch (FlurlHttpException ex) when (ex.StatusCode is 401 or 403)
        {
            throw new InvalidOperationException(
                "ProxyPay recusou o token NAuth ao consultar /graphql (myStore). " +
                "Confirme que o usuário de teste tem acesso ao tenant informado.", ex);
        }
        catch (FlurlHttpException ex)
        {
            throw new InvalidOperationException(
                $"ProxyPay indisponível em {endpoint} (status {ex.StatusCode}).", ex);
        }

        GraphQLResponse? response;
        try
        {
            response = JsonSerializer.Deserialize<GraphQLResponse>(rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"ProxyPay retornou JSON inesperado em {endpoint}. " +
                $"Body recebido: {Truncate(rawBody, 500)}", ex);
        }

        if (response?.Errors is { Count: > 0 } errors)
        {
            var messages = string.Join(" | ", errors.Select(e => e.Message ?? "sem mensagem"));
            throw new InvalidOperationException(
                $"GraphQL do ProxyPay retornou erros ao consultar myStore: {messages}");
        }

        var storeId = response?.Data?.MyStore?.FirstOrDefault()?.StoreId;
        return storeId is > 0 ? storeId.Value : null;
    }

    private static async Task<long> CreateStoreAsync(
        string baseUrl, string nauthToken, string tenant, string email)
    {
        var endpoint = $"{baseUrl}/store";
        CreateStoreResponse? response;
        try
        {
            response = await new FlurlRequest(endpoint)
                .WithHeader("Authorization", $"Basic {nauthToken}")
                .WithHeader("X-Tenant-Id", tenant)
                .PostJsonAsync(new
                {
                    name = "Fortuno ApiTests Store",
                    email,
                    billingStrategy = 1
                })
                .ReceiveJson<CreateStoreResponse>();
        }
        catch (FlurlHttpException ex)
        {
            var body = ex.Call?.Response is null ? "<sem resposta>" : await ex.Call.Response.GetStringAsync();
            throw new InvalidOperationException(
                $"Falha ao criar Store no ProxyPay em {endpoint} (status {ex.StatusCode}). " +
                $"Body: {Truncate(body, 500)}", ex);
        }

        if (response?.StoreId is null or <= 0)
            throw new InvalidOperationException(
                $"ProxyPay retornou resposta sem storeId em {endpoint}.");

        Console.WriteLine($"[ProxyPayStoreResolver] Store criada automaticamente no ProxyPay: StoreId={response.StoreId}.");
        return response.StoreId.Value;
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "<vazio>" : (s.Length <= max ? s : s[..max] + "...");

    private sealed class GraphQLResponse
    {
        public GraphQLData? Data { get; set; }
        public List<GraphQLError>? Errors { get; set; }
    }

    private sealed class GraphQLData
    {
        public List<MyStoreData>? MyStore { get; set; }
    }

    private sealed class MyStoreData
    {
        public long? StoreId { get; set; }
    }

    private sealed class GraphQLError
    {
        public string? Message { get; set; }
    }

    private sealed class CreateStoreResponse
    {
        public long? StoreId { get; set; }
    }
}
