namespace Fortuno.Domain.Models;

public class LotteryCombo
{
    public long LotteryComboId { get; set; }
    public long LotteryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float DiscountValue { get; set; }
    public string DiscountLabel { get; set; } = string.Empty;
    public int QuantityStart { get; set; }
    public int QuantityEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Lottery? Lottery { get; set; }
}
