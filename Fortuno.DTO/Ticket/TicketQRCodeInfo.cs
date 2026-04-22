using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class TicketQRCodeInfo
{
    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("brCode")]
    public string BrCode { get; set; } = string.Empty;

    [JsonPropertyName("brCodeBase64")]
    public string BrCodeBase64 { get; set; } = string.Empty;

    [JsonPropertyName("expiredAt")]
    public DateTime ExpiredAt { get; set; }
}
