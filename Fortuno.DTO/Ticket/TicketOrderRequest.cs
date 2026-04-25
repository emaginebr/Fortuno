using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class TicketOrderRequest
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    /// <summary>
    /// Números escolhidos manualmente (opcional). Int64 → decimal (ex.: "42").
    /// Composed → componentes separados por "-" (ex.: "05-11-28-39-60"); a ordem
    /// dos componentes é irrelevante — backend normaliza ordenando ascendente.
    /// A contagem deve ser ≤ <c>quantity</c>; números faltantes serão sorteados
    /// aleatoriamente no momento do pagamento.
    /// </summary>
    [JsonPropertyName("pickedNumbers")]
    public List<string>? PickedNumbers { get; set; }

    [JsonPropertyName("referralCode")]
    public string? ReferralCode { get; set; }
}
