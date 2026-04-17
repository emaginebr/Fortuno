using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.LotteryCombo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("api/lottery-combos")]
[Authorize]
public class LotteryCombosController : ControllerBase
{
    private readonly ILotteryComboService _combos;

    public LotteryCombosController(ILotteryComboService combos)
    {
        _combos = combos;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LotteryComboInsertInfo dto)
    {
        try
        {
            var info = await _combos.CreateAsync(User.GetCurrentUserId(), dto);
            return CreatedAtAction(nameof(ListByLottery), new { lotteryId = info.LotteryId }, info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPut("{comboId:long}")]
    public async Task<IActionResult> Update(long comboId, [FromBody] LotteryComboUpdateInfo dto)
    {
        try
        {
            var info = await _combos.UpdateAsync(User.GetCurrentUserId(), comboId, dto);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpDelete("{comboId:long}")]
    public async Task<IActionResult> Delete(long comboId)
    {
        try
        {
            await _combos.DeleteAsync(User.GetCurrentUserId(), comboId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("lottery/{lotteryId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListByLottery(long lotteryId)
        => Ok(await _combos.ListByLotteryAsync(lotteryId));
}
