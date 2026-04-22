using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

namespace Fortuno.DTO.Ticket;

public class TicketInfo
{
    [JsonPropertyName("ticketId")]
    public long TicketId { get; set; }

    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("userId")]
    public long UserId { get; set; }

    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    [JsonPropertyName("ticketNumber")]
    public long TicketNumber { get; set; }

    /// <summary>
    /// Representação textual ordenada de <c>TicketNumber</c> respeitando o
    /// <c>NumberType</c> da Lottery. Int64 → decimal direto; Composed → componentes
    /// de 2 dígitos ordenados ascendente e separados por "-" (ex.: "05-11-28-39-60").
    /// Persistido em <c>fortuna_tickets.ticket_value</c> para permitir busca.
    /// </summary>
    [JsonPropertyName("ticketValue")]
    public string TicketValue { get; set; } = string.Empty;

    [JsonPropertyName("refundState")]
    public TicketRefundStateDto RefundState { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
