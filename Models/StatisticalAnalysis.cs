namespace LotteryTracker.API.Models;

public class NumberFrequency
{
    public int Number { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
    public DateTime LastDrawnDate { get; set; }
    public int DaysSinceLastDraw { get; set; }
}

public class StatisticalAnalysis
{
    public Guid Id { get; set; }
    public DateTime AnalyzedDate { get; set; }
    public int TotalDrawings { get; set; }
    public double[] NumberFrequencies { get; set; } = [];
    public double ChiSquareValue { get; set; }
    public double ExpectedChiSquareValue { get; set; }
    public bool IsRandomized { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
