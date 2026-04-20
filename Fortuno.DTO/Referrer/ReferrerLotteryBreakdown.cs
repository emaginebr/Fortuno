using System.Text.Json.Serialization;

namespace Fortuno.DTO.Referrer;

public class ReferrerLotteryBreakdown
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("lotteryName")]
    public string LotteryName { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("purchases")]
    public int Purchases { get; set; }
}
