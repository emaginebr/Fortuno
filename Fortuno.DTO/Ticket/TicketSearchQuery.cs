using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class TicketSearchQuery
{
    [JsonPropertyName("lotteryId")]
    public long? LotteryId { get; set; }

    [JsonPropertyName("number")]
    public long? Number { get; set; }

    [JsonPropertyName("fromDate")]
    public DateTime? FromDate { get; set; }

    [JsonPropertyName("toDate")]
    public DateTime? ToDate { get; set; }
}
