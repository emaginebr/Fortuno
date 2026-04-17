namespace Fortuno.Domain.Models;

public class RaffleAward
{
    public long RaffleAwardId { get; set; }
    public long RaffleId { get; set; }
    public int Position { get; set; }
    public string Description { get; set; } = string.Empty;

    public Raffle? Raffle { get; set; }
}
