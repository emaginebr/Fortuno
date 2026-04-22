using System.Text.Json.Serialization;

namespace Fortuno.DTO.ProxyPay;

public class ProxyPayQRCodeRequest
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("customer")]
    public ProxyPayCustomer Customer { get; set; } = new();

    [JsonPropertyName("items")]
    public List<ProxyPayItem> Items { get; set; } = new();
}
