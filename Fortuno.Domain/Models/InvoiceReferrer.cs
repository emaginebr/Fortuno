namespace Fortuno.Domain.Models;

public class InvoiceReferrer
{
    public long InvoiceReferrerId { get; set; }
    public long InvoiceId { get; set; }
    public long ReferrerUserId { get; set; }
    public long LotteryId { get; set; }
    public float ReferralPercentAtPurchase { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
