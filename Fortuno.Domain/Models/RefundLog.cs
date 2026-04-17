namespace Fortuno.Domain.Models;

public class RefundLog
{
    public long RefundLogId { get; set; }
    public long TicketId { get; set; }
    public long ExecutedByUserId { get; set; }
    public decimal ReferenceValue { get; set; }
    public string? ExternalReference { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
}
