using System.Text.Json.Serialization;

namespace Fortuno.DTO.Purchase;

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
