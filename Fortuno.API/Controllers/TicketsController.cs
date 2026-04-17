using Fortuno.DTO.Common;
using Fortuno.Domain.Interfaces;
using Fortuno.DTO.Ticket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fortuno.API.Controllers;

[ApiController]
[Route("api/tickets")]
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
        [FromQuery] long? number,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var userId = User.GetCurrentUserId();
        var list = await _tickets.ListForUserAsync(userId, new TicketSearchQuery
        {
            LotteryId = lotteryId,
            Number = number,
            FromDate = fromDate,
            ToDate = toDate
        });
        return Ok(list);
    }

    [HttpGet("{ticketId:long}")]
    public async Task<IActionResult> Get(long ticketId)
    {
        var info = await _tickets.GetByIdAsync(ticketId, User.GetCurrentUserId());
        return info is null ? NotFound() : Ok(info);
    }
}
