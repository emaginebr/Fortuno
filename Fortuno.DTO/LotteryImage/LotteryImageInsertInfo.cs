using System.Text.Json.Serialization;

namespace Fortuno.DTO.LotteryImage;

public class LotteryImageInsertInfo
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("imageBase64")]
    public string ImageBase64 { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }
}
