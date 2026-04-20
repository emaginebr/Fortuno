using Flurl.Http;

namespace Fortuno.ApiTests._Fixtures;

public sealed class ApiSessionFixture : IAsyncLifetime
{
    public FlurlClient Client { get; private set; } = default!;
    public long StoreId { get; private set; }
    public string NAuthTenant { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var settings = TestSettings.FromEnvironment();
        NAuthTenant = settings.NAuthTenant;

        var token = await LoginAsync(settings);

        Client = new FlurlClient(settings.ApiBaseUrl)
            .WithHeader("Authorization", $"Basic {token}")
            .WithHeader("X-Tenant-Id", settings.NAuthTenant);

        // Descobre a Store do usuário autenticado via GraphQL do ProxyPay
        // (R-001 v2: elimina dependência de FORTUNO_TEST_STORE_ID).
        StoreId = await ProxyPayStoreResolver.ResolveAsync(
            settings.ProxyPayUrl,
            token,
            settings.NAuthTenant,
            settings.NAuthUser);

        Console.WriteLine(
            $"[ApiSessionFixture] Autenticado no tenant '{settings.NAuthTenant}', " +
            $"StoreId={StoreId} (descoberto via ProxyPay GraphQL).");
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<string> LoginAsync(TestSettings settings)
    {
        try
        {
            var response = await new FlurlRequest($"{settings.NAuthUrl}/user/loginWithEmail")
                .WithHeader("X-Tenant-Id", settings.NAuthTenant)
                .WithHeader("User-Agent", "Fortuno.ApiTests/1.0")
                .WithHeader("X-Device-Fingerprint", "fortuno-api-tests")
                .PostJsonAsync(new
                {
                    email    = settings.NAuthUser,
                    password = settings.NAuthPassword
                })
                .ReceiveJson<LoginResponse>();

            if (string.IsNullOrWhiteSpace(response?.Token))
                throw new InvalidOperationException(
                    $"NAuth retornou resposta sem token em {settings.NAuthUrl}/user/loginWithEmail.");

            return response.Token;
        }
        catch (FlurlHttpException ex) when (ex.StatusCode is 400 or 401)
        {
            throw new InvalidOperationException(
                "Credenciais de teste inválidas — verifique FORTUNO_TEST_NAUTH_USER / PASSWORD / TENANT.", ex);
        }
        catch (FlurlHttpException ex)
        {
            throw new InvalidOperationException(
                $"NAuth indisponível em {settings.NAuthUrl} (status {ex.StatusCode}).", ex);
        }
    }

    private sealed record LoginResponse(string Token);
}
