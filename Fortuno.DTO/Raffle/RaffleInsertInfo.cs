using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

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

public class RaffleUpdateInfo : RaffleInsertInfo { }

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
}
