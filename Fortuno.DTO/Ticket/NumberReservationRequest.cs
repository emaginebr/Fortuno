using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class NumberReservationRequest
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    /// <summary>
    /// Número do ticket em formato texto. Int64 → decimal (ex.: "42").
    /// Composed → componentes separados por "-" (ex.: "05-11-28-39-60").
    /// </summary>
    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; set; } = string.Empty;
}
