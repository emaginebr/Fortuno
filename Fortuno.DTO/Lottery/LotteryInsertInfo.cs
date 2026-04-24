using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;

namespace Fortuno.DTO.Lottery;

public class LotteryInsertInfo
{
    [JsonPropertyName("editionNumber")]
    public int EditionNumber { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("descriptionMd")]
    public string DescriptionMd { get; set; } = string.Empty;

    [JsonPropertyName("rulesMd")]
    public string RulesMd { get; set; } = string.Empty;

    [JsonPropertyName("privacyPolicyMd")]
    public string PrivacyPolicyMd { get; set; } = string.Empty;

    [JsonPropertyName("ticketPrice")]
    public decimal TicketPrice { get; set; }

    [JsonPropertyName("totalPrizeValue")]
    public decimal TotalPrizeValue { get; set; }

    [JsonPropertyName("ticketMin")]
    public int TicketMin { get; set; }

    [JsonPropertyName("ticketMax")]
    public int TicketMax { get; set; }

    [JsonPropertyName("ticketNumIni")]
    public long TicketNumIni { get; set; } = 1;

    [JsonPropertyName("ticketNumEnd")]
    public long TicketNumEnd { get; set; }

    [JsonPropertyName("numberType")]
    public NumberTypeDto NumberType { get; set; } = NumberTypeDto.Int64;

    [JsonPropertyName("numberValueMin")]
    public int NumberValueMin { get; set; }

    [JsonPropertyName("numberValueMax")]
    public int NumberValueMax { get; set; }

    [JsonPropertyName("referralPercent")]
    public float ReferralPercent { get; set; }
}
