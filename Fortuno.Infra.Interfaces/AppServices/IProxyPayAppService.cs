using Fortuno.DTO.ProxyPay;

namespace Fortuno.Infra.Interfaces.AppServices;

public interface IProxyPayAppService
{
    Task<ProxyPayStoreInfo?> GetStoreAsync(long storeId);
    Task<ProxyPayQRCodeResponse> CreateQRCodeAsync(ProxyPayQRCodeRequest request);
    Task<ProxyPayQRCodeStatusResponse?> GetQRCodeStatusAsync(long invoiceId);
}
