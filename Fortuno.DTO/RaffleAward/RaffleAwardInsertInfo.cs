using System.Text.Json.Serialization;

namespace Fortuno.DTO.RaffleAward;

public class RaffleAwardInsertInfo
{
    [JsonPropertyName("raffleId")]
    public long RaffleId { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class RaffleAwardUpdateInfo
{
    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

public class RaffleAwardInfo
{
    [JsonPropertyName("raffleAwardId")]
    public long RaffleAwardId { get; set; }

    [JsonPropertyName("raffleId")]
    public long RaffleId { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
