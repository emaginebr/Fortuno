using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("api/commissions")]
[Authorize]
public class CommissionsController : ControllerBase
{
    private readonly IReferralService _referrals;

    public CommissionsController(IReferralService referrals)
    {
        _referrals = referrals;
    }

    [HttpGet("lottery/{lotteryId:long}")]
    public async Task<IActionResult> ByLottery(long lotteryId)
    {
        try
        {
            var panel = await _referrals.GetPayablesForLotteryAsync(User.GetCurrentUserId(), lotteryId);
            return Ok(panel);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
    }
}
