using Microsoft.Extensions.Configuration;

namespace Fortuno.ApiTests._Fixtures;

public sealed record TestSettings(
    string ApiBaseUrl,
    string NAuthUrl,
    string NAuthTenant,
    string NAuthUser,
    string NAuthPassword,
    string ProxyPayUrl)
{
    public static TestSettings FromEnvironment()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Tests.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables(prefix: "FORTUNO_TEST_")
            .Build();

        string Read(string jsonKey, string envKey) =>
            Environment.GetEnvironmentVariable($"FORTUNO_TEST_{envKey}")
                ?? config[$"FortunoTests:{jsonKey}"]
                ?? string.Empty;

        var apiBaseUrl   = Read("ApiBaseUrl",   "API_BASE_URL");
        var nauthUrl     = Read("NAuthUrl",     "NAUTH_URL");
        var nauthTenant  = Read("NAuthTenant",  "NAUTH_TENANT");
        var nauthUser    = Read("NAuthUser",    "NAUTH_USER");
        var nauthPass    = Read("NAuthPassword","NAUTH_PASSWORD");
        var proxyPayUrl  = Read("ProxyPayUrl",  "PROXYPAY_URL");

        Require(nameof(apiBaseUrl),  apiBaseUrl);
        Require(nameof(nauthUrl),    nauthUrl);
        Require(nameof(nauthTenant), nauthTenant);
        Require(nameof(nauthUser),   nauthUser);
        Require(nameof(nauthPass),   nauthPass);
        Require(nameof(proxyPayUrl), proxyPayUrl);

        return new TestSettings(apiBaseUrl, nauthUrl, nauthTenant, nauthUser, nauthPass, proxyPayUrl);
    }

    private static void Require(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Variável de configuração obrigatória ausente: {name}. " +
                "Defina via FORTUNO_TEST_* ou appsettings.Tests.json (ver appsettings.Tests.example.json).");
    }
}
