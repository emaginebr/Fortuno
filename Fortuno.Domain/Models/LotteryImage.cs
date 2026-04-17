namespace Fortuno.Domain.Models;

public class LotteryImage
{
    public long LotteryImageId { get; set; }
    public long LotteryId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Lottery? Lottery { get; set; }
}
