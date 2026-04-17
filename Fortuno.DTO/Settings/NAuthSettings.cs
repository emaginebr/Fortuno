namespace Fortuno.DTO.Settings;

public class NAuthSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Tenant { get; set; } = "fortuna";
    public string ApiKey { get; set; } = string.Empty;
}
