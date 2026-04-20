using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Refund;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("refunds")]
[Authorize]
public class RefundsController : ControllerBase
{
    private readonly IRefundService _refunds;

    public RefundsController(IRefundService refunds)
    {
        _refunds = refunds;
    }

    [HttpGet("pending/{lotteryId:long}")]
    public async Task<IActionResult> Pending(long lotteryId)
    {
        try
        {
            var list = await _refunds.ListPendingByLotteryAsync(User.GetCurrentUserId(), lotteryId);
            return Ok(list);
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("mark-refunded")]
    public async Task<IActionResult> MarkRefunded([FromBody] RefundStatusChangeRequest request)
    {
        try
        {
            var count = await _refunds.MarkRefundedAsync(User.GetCurrentUserId(), request);
            return Ok(new { updated = count });
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }
}
