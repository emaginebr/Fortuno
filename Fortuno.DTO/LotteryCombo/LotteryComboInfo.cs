using System.Text.Json.Serialization;

namespace Fortuno.DTO.LotteryCombo;

public class LotteryComboInfo : LotteryComboInsertInfo
{
    [JsonPropertyName("lotteryComboId")]
    public long LotteryComboId { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
