namespace Fortuno.Domain.Models;

public class TicketOrderNumber
{
    public long TicketOrderNumberId { get; set; }
    public long TicketOrderId { get; set; }
    public long TicketNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TicketOrder Order { get; set; } = null!;
}
