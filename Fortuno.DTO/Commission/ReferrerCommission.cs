using System.Text.Json.Serialization;

namespace Fortuno.DTO.Commission;

public class ReferrerCommission
{
    [JsonPropertyName("referrerUserId")]
    public long ReferrerUserId { get; set; }

    [JsonPropertyName("referrerName")]
    public string? ReferrerName { get; set; }

    [JsonPropertyName("referralCode")]
    public string ReferralCode { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("purchases")]
    public int Purchases { get; set; }
}
