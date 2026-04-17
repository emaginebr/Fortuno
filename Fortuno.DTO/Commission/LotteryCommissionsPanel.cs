using System.Text.Json.Serialization;

namespace Fortuno.DTO.Commission;

public class LotteryCommissionsPanel
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("lotteryName")]
    public string LotteryName { get; set; } = string.Empty;

    [JsonPropertyName("totalPayable")]
    public decimal TotalPayable { get; set; }

    [JsonPropertyName("byReferrer")]
    public List<ReferrerCommission> ByReferrer { get; set; } = new();

    [JsonPropertyName("note")]
    public string Note { get; set; } =
        "Painel apenas de consulta. O Fortuno não efetua o pagamento — a liquidação é feita pelo dono da Lottery fora do sistema.";
}

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
