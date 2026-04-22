using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class TicketSearchQuery
{
    [JsonPropertyName("lotteryId")]
    public long? LotteryId { get; set; }

    [JsonPropertyName("number")]
    public long? Number { get; set; }

    /// <summary>Filtro por <c>ticket_value</c> (string formatada e ordenada).</summary>
    [JsonPropertyName("ticketValue")]
    public string? TicketValue { get; set; }

    [JsonPropertyName("fromDate")]
    public DateTime? FromDate { get; set; }

    [JsonPropertyName("toDate")]
    public DateTime? ToDate { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}
