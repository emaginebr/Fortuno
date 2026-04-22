using System.Text.Json.Serialization;
using Fortuno.DTO.Enums;
using Fortuno.DTO.LotteryCombo;
using Fortuno.DTO.LotteryImage;
using Fortuno.DTO.Raffle;

namespace Fortuno.DTO.Lottery;

public class LotteryInfo
{
    [JsonPropertyName("lotteryId")]
    public long LotteryId { get; set; }

    [JsonPropertyName("storeId")]
    public long StoreId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

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
    public long TicketNumIni { get; set; }

    [JsonPropertyName("ticketNumEnd")]
    public long TicketNumEnd { get; set; }

    [JsonPropertyName("numberType")]
    public NumberTypeDto NumberType { get; set; }

    [JsonPropertyName("numberValueMin")]
    public int NumberValueMin { get; set; }

    [JsonPropertyName("numberValueMax")]
    public int NumberValueMax { get; set; }

    [JsonPropertyName("referralPercent")]
    public float ReferralPercent { get; set; }

    [JsonPropertyName("status")]
    public LotteryStatusDto Status { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("images")]
    public List<LotteryImageInfo> Images { get; set; } = new();

    [JsonPropertyName("combos")]
    public List<LotteryComboInfo> Combos { get; set; } = new();

    [JsonPropertyName("raffles")]
    public List<RaffleInfo> Raffles { get; set; } = new();
}
