using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Lottery;
using Fortuno.Infra.Interfaces.AppServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("api/lotteries")]
[Authorize]
public class LotteriesController : ControllerBase
{
    private readonly ILotteryService _lotteries;
    private readonly IZToolsAppService _zTools;

    public LotteriesController(ILotteryService lotteries, IZToolsAppService zTools)
    {
        _lotteries = lotteries;
        _zTools = zTools;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LotteryInsertInfo dto)
    {
        try
        {
            var info = await _lotteries.CreateAsync(User.GetCurrentUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { lotteryId = info.LotteryId }, info);
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("{lotteryId:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(long lotteryId)
    {
        var info = await _lotteries.GetByIdAsync(lotteryId);
        return info is null ? NotFound() : Ok(info);
    }

    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var info = await _lotteries.GetBySlugAsync(slug);
        return info is null ? NotFound() : Ok(info);
    }

    [HttpGet("store/{storeId:long}")]
    public async Task<IActionResult> ListByStore(long storeId)
        => Ok(await _lotteries.ListByStoreAsync(storeId));

    [HttpPut("{lotteryId:long}")]
    public async Task<IActionResult> Update(long lotteryId, [FromBody] LotteryUpdateInfo dto)
    {
        try
        {
            var info = await _lotteries.UpdateAsync(User.GetCurrentUserId(), lotteryId, dto);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{lotteryId:long}/publish")]
    public async Task<IActionResult> Publish(long lotteryId)
    {
        try
        {
            var info = await _lotteries.PublishAsync(User.GetCurrentUserId(), lotteryId);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{lotteryId:long}/close")]
    public async Task<IActionResult> Close(long lotteryId)
    {
        try
        {
            var info = await _lotteries.CloseAsync(User.GetCurrentUserId(), lotteryId);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("{lotteryId:long}/cancel")]
    public async Task<IActionResult> Cancel(long lotteryId, [FromBody] LotteryCancelRequest request)
    {
        try
        {
            var info = await _lotteries.CancelAsync(User.GetCurrentUserId(), lotteryId, request);
            return Ok(info);
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("{lotteryId:long}/possibilities")]
    [AllowAnonymous]
    public async Task<IActionResult> Possibilities(long lotteryId)
    {
        try
        {
            var total = await _lotteries.CalculatePossibilitiesAsync(lotteryId);
            return Ok(new { lotteryId, totalPossibilities = total });
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("{lotteryId:long}/rules.pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> RulesPdf(long lotteryId)
    {
        var info = await _lotteries.GetByIdAsync(lotteryId);
        if (info is null) return NotFound();
        var pdf = await _zTools.GeneratePdfFromMarkdownAsync(info.RulesMd, $"Regras — {info.Name}");
        return File(pdf, "application/pdf", $"lottery-{lotteryId}-rules.pdf");
    }

    [HttpGet("{lotteryId:long}/privacy-policy.pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> PrivacyPolicyPdf(long lotteryId)
    {
        var info = await _lotteries.GetByIdAsync(lotteryId);
        if (info is null) return NotFound();
        var pdf = await _zTools.GeneratePdfFromMarkdownAsync(info.PrivacyPolicyMd, $"Política de Privacidade — {info.Name}");
        return File(pdf, "application/pdf", $"lottery-{lotteryId}-privacy.pdf");
    }
}
