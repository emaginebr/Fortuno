namespace Fortuno.DTO.Settings;

public class ProxyPaySettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Tenant { get; set; } = "fortuna";
    public string ApiKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
