using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("referrals")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referrals;
    private readonly IUserReferrerService _referrerService;

    public ReferralsController(IReferralService referrals, IUserReferrerService referrerService)
    {
        _referrals = referrals;
        _referrerService = referrerService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Mine()
    {
        var userId = User.GetCurrentUserId();
        var panel = await _referrals.GetEarningsForReferrerAsync(userId);
        return Ok(panel);
    }

    [HttpGet("code/me")]
    public async Task<IActionResult> MyCode()
    {
        var userId = User.GetCurrentUserId();
        var code = await _referrerService.GetOrCreateCodeForUserAsync(userId);
        return Ok(new { referralCode = code });
    }
}
