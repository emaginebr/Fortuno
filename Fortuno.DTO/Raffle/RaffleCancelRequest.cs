using System.Text.Json.Serialization;

namespace Fortuno.DTO.Raffle;

public class RaffleCancelRequest
{
    [JsonPropertyName("redistributions")]
    public List<AwardRedistribution> Redistributions { get; set; } = new();
}

public class AwardRedistribution
{
    [JsonPropertyName("raffleAwardId")]
    public long RaffleAwardId { get; set; }

    [JsonPropertyName("targetRaffleId")]
    public long TargetRaffleId { get; set; }
}
