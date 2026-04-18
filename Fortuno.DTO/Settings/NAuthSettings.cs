namespace Fortuno.DTO.Settings;

public class NAuthSettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string TenantId { get; set; } = "fortuna";
    public string JwtSecret { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}
