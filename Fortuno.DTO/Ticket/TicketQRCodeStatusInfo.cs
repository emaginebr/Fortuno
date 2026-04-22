using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class TicketQRCodeStatusInfo
{
    /// <summary>
    /// Valor inteiro do <c>TicketOrderStatus</c> (domain):
    /// 1=Pending, 2=Sent, 3=Paid, 4=Overdue, 5=Cancelled, 6=Expired.
    /// <c>null</c> quando o status é desconhecido ou o provedor está indisponível.
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("expiredAt")]
    public DateTime? ExpiredAt { get; set; }

    [JsonPropertyName("brCode")]
    public string? BrCode { get; set; }

    [JsonPropertyName("brCodeBase64")]
    public string? BrCodeBase64 { get; set; }

    [JsonPropertyName("tickets")]
    public List<TicketInfo>? Tickets { get; set; }
}
