namespace Fortuno.Domain.Models;

public class WebhookEvent
{
    public long WebhookEventId { get; set; }
    public long InvoiceId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public string? PayloadHash { get; set; }
}
