using System.Security.Cryptography;
using System.Text;
using Fortuno.DTO.Settings;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Fortuno.API.Filters;

/// <summary>
/// Valida o header X-ProxyPay-Signature (HMAC-SHA256 do corpo bruto)
/// antes do controller processar o webhook (FR-029b).
/// </summary>
public class ProxyPayWebhookHmacFilter : IAsyncActionFilter
{
    private readonly ProxyPaySettings _settings;

    public ProxyPayWebhookHmacFilter(IOptions<FortunoSettings> options)
    {
        _settings = options.Value.ProxyPay;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        request.EnableBuffering();

        var headerSignature = request.Headers["X-ProxyPay-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerSignature))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (!Validate(body, headerSignature!, _settings.WebhookSecret))
        {
            context.Result = new Microsoft.AspNetCore.Mvc.UnauthorizedResult();
            return;
        }

        await next();
    }

    private static bool Validate(string body, string headerSignature, string secret)
    {
        if (string.IsNullOrWhiteSpace(secret)) return false;

        var hex = headerSignature.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
            ? headerSignature.Substring("sha256=".Length)
            : headerSignature;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var computedHex = Convert.ToHexString(computed);
        return string.Equals(computedHex, hex, StringComparison.OrdinalIgnoreCase);
    }
}
