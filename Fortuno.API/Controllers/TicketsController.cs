using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("tickets")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly ITicketService _tickets;

    public TicketsController(ITicketService tickets)
    {
        _tickets = tickets;
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(
        [FromQuery] long? lotteryId,
        [FromQuery] string? number,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.GetCurrentUserId();
        var result = await _tickets.ListForUserAsync(userId, new TicketSearchQuery
        {
            LotteryId = lotteryId,
            Number = number,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        });
        return Ok(result);
    }

    [HttpGet("{ticketId:long}")]
    public async Task<IActionResult> Get(long ticketId)
    {
        var info = await _tickets.GetByIdAsync(ticketId, User.GetCurrentUserId());
        return Ok(info);
    }

    [HttpPost("qrcode")]
    public async Task<IActionResult> CreateQRCode([FromBody] TicketOrderRequest request)
    {
        try
        {
            var info = await _tickets.CreateQRCodeAsync(User.GetCurrentUserId(), request);
            return StatusCode(201, info);
        }
        catch (KeyNotFoundException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpPost("reserve-number")]
    public async Task<IActionResult> ReserveNumber([FromBody] NumberReservationRequest request)
    {
        try
        {
            var result = await _tickets.ReserveNumberAsync(User.GetCurrentUserId(), request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }

    [HttpGet("qrcode/{invoiceId:long}/status")]
    public async Task<IActionResult> CheckQRCodeStatus(long invoiceId)
    {
        try
        {
            var info = await _tickets.CheckQRCodeStatusAsync(invoiceId);
            return Ok(info);
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.Fail(ex.Message)); }
    }
}
