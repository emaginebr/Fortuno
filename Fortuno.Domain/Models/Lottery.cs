using Fortuno.Domain.Enums;

namespace Fortuno.Domain.Models;

public class Lottery
{
    public long LotteryId { get; set; }
    public long StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DescriptionMd { get; set; } = string.Empty;
    public string RulesMd { get; set; } = string.Empty;
    public string PrivacyPolicyMd { get; set; } = string.Empty;
    public decimal TicketPrice { get; set; }
    public decimal TotalPrizeValue { get; set; }
    public int TicketMin { get; set; }
    public int TicketMax { get; set; }
    public long TicketNumIni { get; set; } = 1;
    public long TicketNumEnd { get; set; }
    public NumberType NumberType { get; set; } = NumberType.Int64;
    public int NumberValueMin { get; set; }
    public int NumberValueMax { get; set; }
    public float ReferralPercent { get; set; }
    public LotteryStatus Status { get; set; } = LotteryStatus.Draft;
    public string? CancelReason { get; set; }
    public long? CancelledByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<LotteryImage> Images { get; set; } = new();
    public List<LotteryCombo> Combos { get; set; } = new();
    public List<Raffle> Raffles { get; set; } = new();
    public List<Ticket> Tickets { get; set; } = new();
}
