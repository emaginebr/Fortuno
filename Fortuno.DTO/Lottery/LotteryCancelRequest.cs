using System.Text.Json.Serialization;

namespace Fortuno.DTO.Lottery;

public class LotteryCancelRequest
{
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
}
