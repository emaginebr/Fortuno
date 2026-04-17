namespace Fortuno.Domain.Models;

public class NumberReservation
{
    public long ReservationId { get; set; }
    public long LotteryId { get; set; }
    public long UserId { get; set; }
    public long? InvoiceId { get; set; }
    public long TicketNumber { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
