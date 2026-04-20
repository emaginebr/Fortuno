using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

namespace Fortuno.DTO.Purchase;

public class PurchasePreviewRequest
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("mode")]
    public PurchaseAssignmentModeDto Mode { get; set; } = PurchaseAssignmentModeDto.Random;

    [JsonPropertyName("pickedNumbers")]
    public List<long>? PickedNumbers { get; set; }

    [JsonPropertyName("referralCode")]
    public string? ReferralCode { get; set; }
}
