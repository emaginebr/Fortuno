using Fortuno.API.Filters;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Webhook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("webhooks/proxypay")]
[AllowAnonymous]
public class WebhooksController : ControllerBase
{
    private readonly IPurchaseService _purchases;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IPurchaseService purchases, ILogger<WebhooksController> logger)
    {
        _purchases = purchases;
        _logger = logger;
    }

    [HttpPost("invoice-paid")]
    [ServiceFilter(typeof(ProxyPayWebhookHmacFilter))]
    public async Task<IActionResult> InvoicePaid([FromBody] ProxyPayWebhookPayload payload)
    {
        try
        {
            await _purchases.ProcessPaidWebhookAsync(payload);
            return Ok(new { status = "processed" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Webhook com tenant inválido");
            return Unauthorized();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro processando webhook do ProxyPay");
            return StatusCode(500, new { status = "error", message = ex.Message });
        }
    }
}
