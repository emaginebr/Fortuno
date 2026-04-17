using Fortuno.Domain.Enums;

namespace Fortuno.Domain.Models;

public class Raffle
{
    public long RaffleId { get; set; }
    public long LotteryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DescriptionMd { get; set; }
    public DateTime RaffleDatetime { get; set; }
    public string? VideoUrl { get; set; }
    public bool IncludePreviousWinners { get; set; }
    public RaffleStatus Status { get; set; } = RaffleStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Lottery? Lottery { get; set; }
    public List<RaffleAward> Awards { get; set; } = new();
    public List<RaffleWinner> Winners { get; set; } = new();
}
