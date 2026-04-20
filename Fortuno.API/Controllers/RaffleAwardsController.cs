using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.RaffleAward;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("raffle-awards")]
[Authorize]
public class RaffleAwardsController : ControllerBase
{
    private readonly IRaffleAwardService _awards;

    public RaffleAwardsController(IRaffleAwardService awards)
    {
        _awards = awards;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RaffleAwardInsertInfo dto)
    {
        try
        {
            var info = await _awards.CreateAsync(User.GetCurrentUserId(), dto);
            return CreatedAtAction(nameof(ListByRaffle), new { raffleId = info.RaffleId }, info);
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPut("{awardId:long}")]
    public async Task<IActionResult> Update(long awardId, [FromBody] RaffleAwardUpdateInfo dto)
    {
        try
        {
            var info = await _awards.UpdateAsync(User.GetCurrentUserId(), awardId, dto);
            return Ok(info);
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpDelete("{awardId:long}")]
    public async Task<IActionResult> Delete(long awardId)
    {
        try
        {
            await _awards.DeleteAsync(User.GetCurrentUserId(), awardId);
            return NoContent();
        }
        catch (KeyNotFoundException) { return Ok(null); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ListByRaffle([FromQuery] long raffleId)
        => Ok(await _awards.ListByRaffleAsync(raffleId));
}
