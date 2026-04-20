using System.Text.Json.Serialization;

namespace Fortuno.DTO.Raffle;

public class RaffleCancelRequest
{
    [JsonPropertyName("redistributions")]
    public List<AwardRedistribution> Redistributions { get; set; } = new();
}
