using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Purchase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("purchases")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchases;

    public PurchasesController(IPurchaseService purchases)
    {
        _purchases = purchases;
    }

    [HttpPost("preview")]
    [AllowAnonymous]
    public async Task<IActionResult> Preview([FromBody] PurchasePreviewRequest request)
    {
        try
        {
            var currentUserId = User.TryGetCurrentUserId();
            var info = await _purchases.PreviewAsync(currentUserId, request);
            return Ok(info);
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("confirm")]
    [Authorize]
    public async Task<IActionResult> Confirm([FromBody] PurchaseConfirmRequest request)
    {
        try
        {
            var info = await _purchases.ConfirmAsync(User.GetCurrentUserId(), request);
            return StatusCode(201, info);
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }
}
