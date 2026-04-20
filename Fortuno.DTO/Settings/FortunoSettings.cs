namespace Fortuno.DTO.Settings;

public class FortunoSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public NAuthSettings NAuth { get; set; } = new();
    public ProxyPaySettings ProxyPay { get; set; } = new();
    public ZToolsSettings ZTools { get; set; } = new();
    public CorsSettings Cors { get; set; } = new();
}
