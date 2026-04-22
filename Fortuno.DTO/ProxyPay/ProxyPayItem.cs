using System.Text.Json.Serialization;

namespace Fortuno.DTO.ProxyPay;

public class ProxyPayItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("discount")]
    public decimal Discount { get; set; }
}
