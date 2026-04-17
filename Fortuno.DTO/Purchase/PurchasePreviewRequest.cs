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

public class PurchaseConfirmRequest : PurchasePreviewRequest { }

public class PurchaseConfirmResponse
{
    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    [JsonPropertyName("pixQrCode")]
    public string? PixQrCode { get; set; }

    [JsonPropertyName("pixCopyPaste")]
    public string? PixCopyPaste { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }
}
