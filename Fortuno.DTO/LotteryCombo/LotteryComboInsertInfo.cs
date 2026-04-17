using System.Text.Json.Serialization;

namespace Fortuno.DTO.LotteryCombo;

public class LotteryComboInsertInfo
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("discountValue")]
    public float DiscountValue { get; set; }

    [JsonPropertyName("discountLabel")]
    public string DiscountLabel { get; set; } = string.Empty;

    [JsonPropertyName("quantityStart")]
    public int QuantityStart { get; set; }

    [JsonPropertyName("quantityEnd")]
    public int QuantityEnd { get; set; }
}

public class LotteryComboUpdateInfo : LotteryComboInsertInfo { }

public class LotteryComboInfo : LotteryComboInsertInfo
{
    [JsonPropertyName("lotteryComboId")]
    public long LotteryComboId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
