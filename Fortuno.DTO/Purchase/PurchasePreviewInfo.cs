using System.Text.Json.Serialization;

namespace Fortuno.DTO.Purchase;

public class PurchasePreviewInfo
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("discountValue")]
    public decimal DiscountValue { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("applicableCombo")]
    public string? ApplicableCombo { get; set; }

    [JsonPropertyName("availableTickets")]
    public long AvailableTickets { get; set; }

    [JsonPropertyName("referrerUserId")]
    public long? ReferrerUserId { get; set; }

    [JsonPropertyName("referrerIsSelf")]
    public bool ReferrerIsSelf { get; set; }
}
