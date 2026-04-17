using System.Text.Json.Serialization;

namespace Fortuno.DTO.Lottery;

public class LotteryUpdateInfo : LotteryInsertInfo
{
    [JsonPropertyName("slug")]
    public string? Slug { get; set; }
}
