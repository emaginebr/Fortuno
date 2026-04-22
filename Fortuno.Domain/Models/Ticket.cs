using Fortuno.Domain.Enums;

namespace Fortuno.Domain.Models;

public class Ticket
{
    public long TicketId { get; set; }
    public long LotteryId { get; set; }
    public long UserId { get; set; }
    public long InvoiceId { get; set; }
    public long TicketNumber { get; set; }

    // Representação textual ordenada + zero-padded do TicketNumber, computada
    // pela NumberCompositionService.Format no momento da emissão.
    // Persistida para permitir busca por string (ex.: "05-11-28-39-60").
    public string TicketValue { get; set; } = string.Empty;

    public TicketRefundState RefundState { get; set; } = TicketRefundState.None;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lottery? Lottery { get; set; }
}
