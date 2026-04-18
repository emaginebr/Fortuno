namespace Fortuno.DTO.Settings;

public class ProxyPaySettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string TenantId { get; set; } = "fortuno";
    public string WebhookSecret { get; set; } = string.Empty;
}
