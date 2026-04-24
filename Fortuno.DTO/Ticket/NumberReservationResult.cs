using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

namespace Fortuno.DTO.Ticket;

public class NumberReservationResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("status")]
    public NumberReservationStatusDto Status { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; set; } = string.Empty;

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }
}
