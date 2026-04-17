namespace Fortuno.Infra.Interfaces.AppServices;

public interface IProxyPayAppService
{
    Task<ProxyPayStoreInfo?> GetStoreAsync(long storeId);
    Task<ProxyPayInvoiceInfo> CreateInvoiceAsync(ProxyPayCreateInvoiceRequest request);
    Task<ProxyPayInvoiceInfo?> GetInvoiceAsync(long invoiceId);
}

public class ProxyPayStoreInfo
{
    public long StoreId { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
}

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

public class ProxyPayCreateInvoiceRequest
{
    public long StoreId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
