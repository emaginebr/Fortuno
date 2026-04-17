namespace Fortuno.Domain.Models;

public class UserReferrer
{
    public long UserReferrerId { get; set; }
    public long UserId { get; set; }
    public string ReferralCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
