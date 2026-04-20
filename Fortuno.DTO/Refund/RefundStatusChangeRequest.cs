using System.Text.Json.Serialization;

namespace Fortuno.DTO.Refund;

public class RefundStatusChangeRequest
{
    [JsonPropertyName("ticketIds")]
    public List<long> TicketIds { get; set; } = new();

    [JsonPropertyName("externalReference")]
    public string? ExternalReference { get; set; }
}
