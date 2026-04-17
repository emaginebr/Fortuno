using Fortuno.DTO.Ticket;

namespace Fortuno.Domain.Interfaces;

public interface ITicketService
{
    Task<List<TicketInfo>> ListForUserAsync(long userId, TicketSearchQuery query);
    Task<TicketInfo?> GetByIdAsync(long ticketId, long currentUserId);
}
