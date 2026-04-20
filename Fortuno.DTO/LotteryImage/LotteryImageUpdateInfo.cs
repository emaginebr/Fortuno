using System.Text.Json.Serialization;

namespace Fortuno.DTO.LotteryImage;

public class LotteryImageUpdateInfo
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }
}
