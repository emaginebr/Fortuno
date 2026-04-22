using System.Text.Json.Serialization;

namespace Fortuno.DTO.ProxyPay;

public class ProxyPayQRCodeStatusResponse
{
    [JsonPropertyName("invoiceId")]
    public long InvoiceId { get; set; }

    /// <summary>
    /// Status devolvido pelo ProxyPay como inteiro.
    /// Mapeamento para o enum interno acontece no <c>TicketService</c>.
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("paidAt")]
    public DateTime? PaidAt { get; set; }
}
