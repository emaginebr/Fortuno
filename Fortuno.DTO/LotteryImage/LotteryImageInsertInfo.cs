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

public class LotteryImageUpdateInfo
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("displayOrder")]
    public int DisplayOrder { get; set; }
}

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
