using System.Text.Json.Serialization;

namespace Fortuno.DTO.RaffleWinner;

public class RaffleWinnerInfo
{
    [JsonPropertyName("raffleWinnerId")]
    public long RaffleWinnerId { get; set; }

    [JsonPropertyName("raffleId")]
    public long RaffleId { get; set; }

    [JsonPropertyName("raffleAwardId")]
    public long RaffleAwardId { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("prizeText")]
    public string PrizeText { get; set; } = string.Empty;

    [JsonPropertyName("ticketId")]
    public long? TicketId { get; set; }

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }

    [JsonPropertyName("ticketNumber")]
    public long? TicketNumber { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
