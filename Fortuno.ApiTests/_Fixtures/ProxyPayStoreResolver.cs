using Flurl.Http;

namespace Fortuno.ApiTests._Fixtures;

/// <summary>
/// Resolve o <c>StoreId</c> do usuário autenticado consultando o GraphQL do ProxyPay.
/// Elimina a necessidade de fixar <c>FORTUNO_TEST_STORE_ID</c> no ambiente (R-001 v2).
/// </summary>
internal static class ProxyPayStoreResolver
{
    private const string Query = "{ myStore { storeId } }";

    public static async Task<long> ResolveAsync(
        string proxyPayUrl,
        string nauthToken,
        string tenant)
    {
        GraphQLResponse? response;
        try
        {
            response = await new FlurlRequest($"{proxyPayUrl.TrimEnd('/')}/graphql")
                .WithHeader("Authorization", $"Basic {nauthToken}")
                .WithHeader("X-Tenant-Id", tenant)
                .PostJsonAsync(new { query = Query })
                .ReceiveJson<GraphQLResponse>();
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
                $"ProxyPay indisponível em {proxyPayUrl}/graphql (status {ex.StatusCode}).", ex);
        }

        if (response?.Errors is { Count: > 0 } errors)
        {
            var messages = string.Join(" | ", errors.Select(e => e.Message ?? "sem mensagem"));
            throw new InvalidOperationException(
                $"GraphQL do ProxyPay retornou erros ao consultar myStore: {messages}");
        }

        var storeId = response?.Data?.MyStore?.StoreId;
        if (storeId is null or <= 0)
            throw new InvalidOperationException(
                "Consulta `{ myStore { storeId } }` retornou vazio no ProxyPay. " +
                "O usuário de teste precisa ter uma Store associada antes de rodar os ApiTests. " +
                "Crie a Store no ProxyPay (ou troque o usuário) e tente novamente.");

        return storeId.Value;
    }

    private sealed class GraphQLResponse
    {
        public GraphQLData? Data { get; set; }
        public List<GraphQLError>? Errors { get; set; }
    }

    private sealed class GraphQLData
    {
        public MyStoreData? MyStore { get; set; }
    }

    private sealed class MyStoreData
    {
        public long? StoreId { get; set; }
    }

    private sealed class GraphQLError
    {
        public string? Message { get; set; }
    }
}
