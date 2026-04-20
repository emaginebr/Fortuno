using System.Text.Json.Serialization;

namespace Fortuno.DTO.Raffle;

public class RaffleWinnersPreviewRequest
{
    [JsonPropertyName("raffleId")]
    public long RaffleId { get; set; }

    [JsonPropertyName("winningNumbers")]
    public List<long> WinningNumbers { get; set; } = new();
}
