using System.Text.Json.Serialization;

namespace Fortuno.DTO.Raffle;

public class RaffleWinnersPreviewRequest
{
    [JsonPropertyName("raffleId")]
    public long RaffleId { get; set; }

    [JsonPropertyName("winningNumbers")]
    public List<long> WinningNumbers { get; set; } = new();
}

public class RaffleWinnerPreviewRow
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("awardId")]
    public long AwardId { get; set; }

    [JsonPropertyName("prizeText")]
    public string PrizeText { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public long Number { get; set; }

    [JsonPropertyName("ticketId")]
    public long? TicketId { get; set; }

    [JsonPropertyName("userId")]
    public long? UserId { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("userCpfMasked")]
    public string? UserCpfMasked { get; set; }

    [JsonPropertyName("excludedByFlag")]
    public bool ExcludedByFlag { get; set; }

    [JsonPropertyName("notFound")]
    public bool NotFound { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}
