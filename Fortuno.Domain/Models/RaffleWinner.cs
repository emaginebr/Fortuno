namespace Fortuno.Domain.Models;

public class RaffleWinner
{
    public long RaffleWinnerId { get; set; }
    public long RaffleId { get; set; }
    public long RaffleAwardId { get; set; }
    public long? TicketId { get; set; }
    public long? UserId { get; set; }
    public int Position { get; set; }
    public string PrizeText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Raffle? Raffle { get; set; }
    public RaffleAward? RaffleAward { get; set; }
    public Ticket? Ticket { get; set; }
}
