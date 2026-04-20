using System.Text.Json.Serialization;

namespace Fortuno.DTO.Raffle;

public class RaffleInsertInfo
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("descriptionMd")]
    public string? DescriptionMd { get; set; }

    [JsonPropertyName("raffleDatetime")]
    public DateTime RaffleDatetime { get; set; }

    [JsonPropertyName("videoUrl")]
    public string? VideoUrl { get; set; }

    [JsonPropertyName("includePreviousWinners")]
    public bool IncludePreviousWinners { get; set; }
}
