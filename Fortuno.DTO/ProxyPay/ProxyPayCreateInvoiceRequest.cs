namespace Fortuno.DTO.ProxyPay;

public class ProxyPayCreateInvoiceRequest
{
    public long StoreId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
