using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;
using Fortuno.DTO.RaffleAward;

namespace Fortuno.DTO.Raffle;

public class RaffleInfo : RaffleInsertInfo
{
    [JsonPropertyName("raffleId")]
    public long RaffleId { get; set; }

    [JsonPropertyName("status")]
    public RaffleStatusDto Status { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("awards")]
    public List<RaffleAwardInfo> Awards { get; set; } = new();
}
