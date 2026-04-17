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

public class ProxyPayWebhookData
{
    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    [JsonPropertyName("storeId")]
    public long StoreId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("paidAt")]
    public DateTime? PaidAt { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
