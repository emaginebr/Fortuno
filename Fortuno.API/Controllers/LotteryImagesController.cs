using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.LotteryImage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("api/lottery-images")]
[Authorize]
public class LotteryImagesController : ControllerBase
{
    private readonly ILotteryImageService _images;

    public LotteryImagesController(ILotteryImageService images)
    {
        _images = images;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LotteryImageInsertInfo dto)
    {
        try
        {
            var info = await _images.CreateAsync(User.GetCurrentUserId(), dto);
            return CreatedAtAction(nameof(ListByLottery), new { lotteryId = info.LotteryId }, info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPut("{imageId:long}")]
    public async Task<IActionResult> Update(long imageId, [FromBody] LotteryImageUpdateInfo dto)
    {
        try
        {
            var info = await _images.UpdateAsync(User.GetCurrentUserId(), imageId, dto);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpDelete("{imageId:long}")]
    public async Task<IActionResult> Delete(long imageId)
    {
        try
        {
            await _images.DeleteAsync(User.GetCurrentUserId(), imageId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("lottery/{lotteryId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> ListByLottery(long lotteryId)
        => Ok(await _images.ListByLotteryAsync(lotteryId));
}
