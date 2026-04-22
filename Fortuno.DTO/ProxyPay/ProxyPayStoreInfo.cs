namespace Fortuno.DTO.ProxyPay;

public class ProxyPayStoreInfo
{
    public long StoreId { get; set; }
    public long OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}
