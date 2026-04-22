using Fortuno.Domain.Enums;

namespace Fortuno.Domain.Models;

public class TicketOrder
{
    public long TicketOrderId { get; set; }
    public long InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public long UserId { get; set; }
    public long LotteryId { get; set; }
    public int Quantity { get; set; }
    public TicketOrderMode Mode { get; set; }
    public string? ReferralCode { get; set; }
    public float ReferralPercentAtPurchase { get; set; }
    public decimal TotalAmount { get; set; }
    public string? BrCode { get; set; }
    public string? BrCodeBase64 { get; set; }
    public DateTime ExpiredAt { get; set; }
    public TicketOrderStatus Status { get; set; } = TicketOrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Lottery Lottery { get; set; } = null!;
    public List<TicketOrderNumber> Numbers { get; set; } = new();
}
