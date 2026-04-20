using System.Text.Json.Serialization;

namespace Fortuno.DTO.Referrer;

public class ReferrerEarningsPanel
{
    [JsonPropertyName("referralCode")]
    public string ReferralCode { get; set; } = string.Empty;

    [JsonPropertyName("totalPurchases")]
    public int TotalPurchases { get; set; }

    [JsonPropertyName("totalToReceive")]
    public decimal TotalToReceive { get; set; }

    [JsonPropertyName("byLottery")]
    public List<ReferrerLotteryBreakdown> ByLottery { get; set; } = new();

    [JsonPropertyName("note")]
    public string Note { get; set; } =
        "O pagamento destas comissões ocorre fora do sistema. O Fortuno apenas calcula e exibe os valores.";
}
