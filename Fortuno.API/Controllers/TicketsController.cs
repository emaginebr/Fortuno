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
    private readonly ILogger<TicketsController> _logger;

    public TicketsController(ITicketService tickets, ILogger<TicketsController> logger)
    {
        _tickets = tickets;
        _logger = logger;
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
        var userId = User.GetCurrentUserId();
        try
        {
            var info = await _tickets.CreateQRCodeAsync(userId, request);
            return StatusCode(201, info);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "POST /tickets/qrcode 400 — userId={UserId} lotteryId={LotteryId} qty={Qty}: {Message}",
                userId, request.LotteryId, request.Quantity, ex.Message);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "POST /tickets/qrcode 403 — userId={UserId} lotteryId={LotteryId}: {Message}",
                userId, request.LotteryId, ex.Message);
            return StatusCode(403, ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POST /tickets/qrcode 400 — userId={UserId} lotteryId={LotteryId} qty={Qty} picks={Picks}: {Message}",
                userId, request.LotteryId, request.Quantity,
                request.PickedNumbers is null ? "null" : string.Join(",", request.PickedNumbers),
                ex.Message);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST /tickets/qrcode 500 — userId={UserId} lotteryId={LotteryId}: {Message}",
                userId, request.LotteryId, ex.Message);
            throw;
        }
    }

    [HttpPost("reserve-number")]
    public async Task<IActionResult> ReserveNumber([FromBody] NumberReservationRequest request)
    {
        var userId = User.GetCurrentUserId();
        try
        {
            var result = await _tickets.ReserveNumberAsync(userId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "POST /tickets/reserve-number 400 — userId={UserId} lotteryId={LotteryId} number={Number}: {Message}",
                userId, request.LotteryId, request.TicketNumber, ex.Message);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "POST /tickets/reserve-number 400 — userId={UserId} lotteryId={LotteryId} number={Number}: {Message}",
                userId, request.LotteryId, request.TicketNumber, ex.Message);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }

    [HttpGet("qrcode/{invoiceId:long}/status")]
    public async Task<IActionResult> CheckQRCodeStatus(long invoiceId)
    {
        try
        {
            var info = await _tickets.CheckQRCodeStatusAsync(invoiceId);
            return Ok(info);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GET /tickets/qrcode/{InvoiceId}/status 400: {Message}", invoiceId, ex.Message);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
    }
}
