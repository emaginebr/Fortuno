using System.Text.Json.Serialization;

namespace Fortuno.DTO.ProxyPay;

public class ProxyPayCustomer
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    [JsonPropertyName("cellphone")]
    public string Cellphone { get; set; } = string.Empty;
}
