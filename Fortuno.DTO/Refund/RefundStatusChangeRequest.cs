using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

namespace Fortuno.DTO.Refund;

public class RefundStatusChangeRequest
{
    [JsonPropertyName("ticketIds")]
    public List<long> TicketIds { get; set; } = new();

    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }
}

public class PendingRefundTicketInfo
{
    [JsonPropertyName("ticketId")]
    public long TicketId { get; set; }

    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("ticketNumber")]
    public long TicketNumber { get; set; }

    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    [JsonPropertyName("refundState")]
    public TicketRefundStateDto RefundState { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
