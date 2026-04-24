using System.Text.Json.Serialization;

namespace Fortuno.DTO.Ticket;

public class TicketSearchQuery
{
    [JsonPropertyName("lotteryId")]
    public long? LotteryId { get; set; }

    /// <summary>
    /// Filtro por número do ticket em formato texto. Int64 → decimal (ex.: "42").
    /// Composed → componentes separados por "-" (ex.: "05-11-28-39-60"); a ordem
    /// é irrelevante — backend normaliza para match exato contra <c>ticket_value</c>.
    /// </summary>
    [JsonPropertyName("number")]
    public string? Number { get; set; }

    [JsonPropertyName("fromDate")]
    public DateTime? FromDate { get; set; }

    [JsonPropertyName("toDate")]
    public DateTime? ToDate { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 20;
}
