using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

namespace Fortuno.DTO.Ticket;

public class TicketOrderRequest
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("mode")]
    public TicketOrderMode Mode { get; set; } = TicketOrderMode.Random;

    /// <summary>
    /// Lista de números escolhidos em formato texto. Int64 → decimal (ex.: "42").
    /// Composed → componentes separados por "-" (ex.: "05-11-28-39-60"); a ordem
    /// dos componentes é irrelevante — backend normaliza ordenando ascendente.
    /// Obrigatório quando <c>mode = UserPicks</c>.
    /// </summary>
    [JsonPropertyName("pickedNumbers")]
    public List<string>? PickedNumbers { get; set; }

    [JsonPropertyName("referralCode")]
    public string? ReferralCode { get; set; }
}
