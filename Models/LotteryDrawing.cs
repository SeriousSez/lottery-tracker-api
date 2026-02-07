namespace LotteryTracker.API.Models;

public class LotteryDrawing
{
    public Guid Id { get; set; }
    public DateTime DrawDate { get; set; }
    public int[] WinningNumbers { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
