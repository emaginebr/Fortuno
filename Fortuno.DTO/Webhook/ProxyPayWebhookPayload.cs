using System.Text.Json.Serialization;

namespace Fortuno.DTO.Webhook;

public class ProxyPayWebhookPayload
{
    [JsonPropertyName("eventId")]
    public string? EventId { get; set; }

    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; set; }

    [JsonPropertyName("tenant")]
    public string Tenant { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public ProxyPayWebhookData Data { get; set; } = new();
}
