using Fortuno.Domain.Enums;

namespace Fortuno.Domain.Models;

public class Ticket
{
    public long TicketId { get; set; }
    public long LotteryId { get; set; }
    public long UserId { get; set; }
    public long InvoiceId { get; set; }
    public long TicketNumber { get; set; }
    public TicketRefundState RefundState { get; set; } = TicketRefundState.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lottery? Lottery { get; set; }
}
