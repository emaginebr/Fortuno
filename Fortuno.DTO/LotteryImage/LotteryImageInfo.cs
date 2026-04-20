using System.Text.Json.Serialization;

namespace Fortuno.DTO.LotteryImage;

public class LotteryImageInfo
{
    [JsonPropertyName("lotteryImageId")]
    public long LotteryImageId { get; set; }

    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
