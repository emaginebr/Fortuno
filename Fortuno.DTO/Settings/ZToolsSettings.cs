namespace Fortuno.DTO.Settings;

public class ZToolsSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string S3BucketName { get; set; } = string.Empty;
    public string S3Region { get; set; } = "us-east-1";
}
