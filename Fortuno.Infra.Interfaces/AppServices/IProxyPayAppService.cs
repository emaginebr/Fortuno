using Fortuno.DTO.ProxyPay;

namespace Fortuno.Infra.Interfaces.AppServices;

public interface IProxyPayAppService
{
    Task<ProxyPayStoreInfo?> GetStoreAsync(long storeId);
    Task<ProxyPayInvoiceInfo> CreateInvoiceAsync(ProxyPayCreateInvoiceRequest request);
    Task<ProxyPayInvoiceInfo?> GetInvoiceAsync(long invoiceId);
}
