using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Raffle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("api/raffles")]
[Authorize]
public class RafflesController : ControllerBase
{
    private readonly IRaffleService _raffles;

    public RafflesController(IRaffleService raffles)
    {
        _raffles = raffles;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RaffleInsertInfo dto)
    {
        try
        {
            var info = await _raffles.CreateAsync(User.GetCurrentUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { raffleId = info.RaffleId }, info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("{raffleId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long raffleId)
    {
        var info = await _raffles.GetByIdAsync(raffleId);
        return info is null ? NotFound() : Ok(info);
    }

    [HttpGet("lottery/{lotteryId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListByLottery(long lotteryId)
        => Ok(await _raffles.ListByLotteryAsync(lotteryId));

    [HttpPost("{raffleId:long}/winners/preview")]
    public async Task<IActionResult> PreviewWinners(long raffleId, [FromBody] RaffleWinnersPreviewRequest request)
    {
        request.RaffleId = raffleId;
        try
        {
            var rows = await _raffles.PreviewWinnersAsync(User.GetCurrentUserId(), request);
            return Ok(rows);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{raffleId:long}/winners/confirm")]
    public async Task<IActionResult> ConfirmWinners(long raffleId, [FromBody] RaffleWinnersPreviewRequest request)
    {
        request.RaffleId = raffleId;
        try
        {
            var winners = await _raffles.ConfirmWinnersAsync(User.GetCurrentUserId(), request);
            return StatusCode(201, winners);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{raffleId:long}/close")]
    public async Task<IActionResult> Close(long raffleId)
    {
        try
        {
            var info = await _raffles.CloseAsync(User.GetCurrentUserId(), raffleId);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPut("{raffleId:long}")]
    public async Task<IActionResult> Update(long raffleId, [FromBody] RaffleUpdateInfo dto)
    {
        try
        {
            var info = await _raffles.UpdateAsync(User.GetCurrentUserId(), raffleId, dto);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpDelete("{raffleId:long}")]
    public async Task<IActionResult> Delete(long raffleId)
    {
        try
        {
            await _raffles.DeleteAsync(User.GetCurrentUserId(), raffleId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{raffleId:long}/cancel")]
    public async Task<IActionResult> Cancel(long raffleId, [FromBody] RaffleCancelRequest request)
    {
        try
        {
            var info = await _raffles.CancelAsync(User.GetCurrentUserId(), raffleId, request);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }
}
