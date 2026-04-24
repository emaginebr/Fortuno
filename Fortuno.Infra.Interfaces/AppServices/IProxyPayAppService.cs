using Fortuno.DTO.ProxyPay;

namespace Fortuno.Infra.Interfaces.AppServices;

public interface IProxyPayAppService
{
    Task<ProxyPayStoreInfo?> GetStoreAsync(long storeId);
    Task<ProxyPayStoreInfo?> GetMyStoreAsync();
    Task<ProxyPayStoreInfo> CreateStoreAsync(string name, string email);
    Task<ProxyPayStoreInfo> EnsureMyStoreAsync(string name, string email);
    Task<ProxyPayQRCodeResponse> CreateQRCodeAsync(ProxyPayQRCodeRequest request);
    Task<ProxyPayQRCodeStatusResponse?> GetQRCodeStatusAsync(long invoiceId);
}
