using Fortuno.DTO.Common;
using Fortuno.DTO.Ticket;

namespace Fortuno.Domain.Interfaces;

public interface ITicketService
{
    Task<PagedResult<TicketInfo>> ListForUserAsync(long userId, TicketSearchQuery query);
    Task<TicketInfo?> GetByIdAsync(long ticketId, long currentUserId);

    Task<TicketQRCodeInfo> CreateQRCodeAsync(long currentUserId, TicketOrderRequest request);
    Task<TicketQRCodeStatusInfo> CheckQRCodeStatusAsync(long invoiceId);

    Task<NumberReservationResult> ReserveNumberAsync(long currentUserId, NumberReservationRequest request);
}
