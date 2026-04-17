using Fortuno.DTO.Purchase;
using Fortuno.DTO.Webhook;

namespace Fortuno.Domain.Interfaces;

public interface IPurchaseService
{
    Task<PurchasePreviewInfo> PreviewAsync(long? currentUserId, PurchasePreviewRequest request);
    Task<PurchaseConfirmResponse> ConfirmAsync(long currentUserId, PurchaseConfirmRequest request);
    Task ProcessPaidWebhookAsync(ProxyPayWebhookPayload payload);
}
