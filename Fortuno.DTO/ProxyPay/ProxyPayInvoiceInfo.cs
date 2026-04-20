namespace Fortuno.DTO.ProxyPay;

public class ProxyPayInvoiceInfo
{
    public long InvoiceId { get; set; }
    public long StoreId { get; set; }
    public decimal Amount { get; set; }
    public decimal? PaidAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public string? PixQrCode { get; set; }
    public string? PixCopyPaste { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
