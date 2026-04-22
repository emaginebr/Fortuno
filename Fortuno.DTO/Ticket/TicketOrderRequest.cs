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

    [JsonPropertyName("pickedNumbers")]
    public List<long>? PickedNumbers { get; set; }

    [JsonPropertyName("referralCode")]
    public string? ReferralCode { get; set; }
}
