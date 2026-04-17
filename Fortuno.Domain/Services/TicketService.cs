using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Ticket;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepository<Ticket> _tickets;

    public TicketService(ITicketRepository<Ticket> tickets)
    {
        _tickets = tickets;
    }

    public async Task<List<TicketInfo>> ListForUserAsync(long userId, TicketSearchQuery query)
    {
        var list = await _tickets.ListByUserAsync(userId, query.LotteryId, query.Number);
        IEnumerable<Ticket> filtered = list;
        if (query.FromDate.HasValue) filtered = filtered.Where(t => t.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) filtered = filtered.Where(t => t.CreatedAt <= query.ToDate.Value);
        return filtered.Select(MapToDto).ToList();
    }

    public async Task<TicketInfo?> GetByIdAsync(long ticketId, long currentUserId)
    {
        var t = await _tickets.GetByIdAsync(ticketId);
        if (t is null || t.UserId != currentUserId) return null;
        return MapToDto(t);
    }

    private static TicketInfo MapToDto(Ticket t) => new()
    {
        TicketId = t.TicketId,
        LotteryId = t.LotteryId,
        UserId = t.UserId,
        InvoiceId = t.InvoiceId,
        TicketNumber = t.TicketNumber,
        RefundState = (TicketRefundStateDto)t.RefundState,
        CreatedAt = t.CreatedAt
    };
}
