using LotteryTracker.API.Data;
using LotteryTracker.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace LotteryTracker.API.Services;

public interface IStatisticalAnalysisService
{
    Task<StatisticalAnalysis> AnalyzeDrawingsAsync();
    NumberFrequency[] CalculateNumberFrequencies();
    (double chiSquareValue, double expectedValue) PerformChiSquareTest(int[] numbers);
}

public class StatisticalAnalysisService : IStatisticalAnalysisService
{
    private readonly LotteryDbContext _context;
    private readonly ILogger<StatisticalAnalysisService> _logger;

    private const int LOTTERY_NUMBERS_MIN = 1;
    private const int LOTTERY_NUMBERS_MAX = 36; // Danish Lotto uses numbers 1-36

    public StatisticalAnalysisService(LotteryDbContext context, ILogger<StatisticalAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StatisticalAnalysis> AnalyzeDrawingsAsync()
    {
        try
        {
            var drawings = await _context.Drawings.ToListAsync();
            _logger.LogInformation("Analyzing {Count} lottery drawings", drawings.Count);

            var frequencies = CalculateNumberFrequencies();
            var allNumbers = drawings.SelectMany(d => d.WinningNumbers).ToArray();
            var (chiSquare, expected) = PerformChiSquareTest(allNumbers);

            var analysis = new StatisticalAnalysis
            {
                AnalyzedDate = DateTime.UtcNow,
                TotalDrawings = drawings.Count,
                NumberFrequencies = frequencies.Select(f => (double)f.Count).ToArray(),
                ChiSquareValue = chiSquare,
                ExpectedChiSquareValue = expected,
                IsRandomized = IsDataRandomized(chiSquare, expected),
                Notes = GenerateAnalysisNotes(frequencies)
            };

            _context.Analyses.Add(analysis);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Statistical analysis completed. Chi-square: {ChiSquare}, Expected: {Expected}, Randomized: {IsRandomized}",
                chiSquare, expected, analysis.IsRandomized);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing statistical analysis");
            throw;
        }
    }

    public NumberFrequency[] CalculateNumberFrequencies()
    {
        var drawings = _context.Drawings.ToList();
        var frequencies = new Dictionary<int, NumberFrequency>();

        // Initialize frequency dictionary
        for (int i = LOTTERY_NUMBERS_MIN; i <= LOTTERY_NUMBERS_MAX; i++)
        {
            frequencies[i] = new NumberFrequency { Number = i, Count = 0, LastDrawnDate = DateTime.MinValue };
        }

        // Count occurrences
        foreach (var drawing in drawings)
        {
            foreach (var number in drawing.WinningNumbers)
            {
                if (frequencies.ContainsKey(number))
                {
                    frequencies[number].Count++;
                    // Only update LastDrawnDate if this drawing is more recent
                    if (drawing.DrawDate > frequencies[number].LastDrawnDate)
                    {
                        frequencies[number].LastDrawnDate = drawing.DrawDate;
                    }
                }
            }
        }

        // Calculate percentages and days since last draw
        var totalOccurrences = frequencies.Values.Sum(f => f.Count);
        var today = DateTime.UtcNow;

        foreach (var freq in frequencies.Values)
        {
            freq.Percentage = totalOccurrences > 0 ? (freq.Count * 100.0) / totalOccurrences : 0;
            freq.DaysSinceLastDraw = freq.LastDrawnDate == DateTime.MinValue ? int.MaxValue : (int)(today - freq.LastDrawnDate).TotalDays;
        }

        return frequencies.Values.OrderBy(f => f.Number).ToArray();
    }

    public (double chiSquareValue, double expectedValue) PerformChiSquareTest(int[] numbers)
    {
        if (numbers.Length == 0)
            return (0, 0);

        // Expected frequency if numbers are uniformly distributed
        double expectedFrequency = (double)numbers.Length / (LOTTERY_NUMBERS_MAX - LOTTERY_NUMBERS_MIN + 1);

        var observedFrequencies = new int[LOTTERY_NUMBERS_MAX + 1];
        foreach (var number in numbers)
        {
            if (number >= LOTTERY_NUMBERS_MIN && number <= LOTTERY_NUMBERS_MAX)
                observedFrequencies[number]++;
        }

        // Calculate chi-square statistic
        double chiSquare = 0;
        for (int i = LOTTERY_NUMBERS_MIN; i <= LOTTERY_NUMBERS_MAX; i++)
        {
            double observed = observedFrequencies[i];
            double expected = expectedFrequency;
            chiSquare += Math.Pow(observed - expected, 2) / expected;
        }

        // Degrees of freedom = number of categories - 1
        int degreesOfFreedom = (LOTTERY_NUMBERS_MAX - LOTTERY_NUMBERS_MIN + 1) - 1;

        // Critical value for chi-square distribution at 0.05 significance level
        // This is the expected chi-square value for random data
        double expectedChiSquare = GetChiSquareCriticalValue(degreesOfFreedom);

        return (chiSquare, expectedChiSquare);
    }

    private bool IsDataRandomized(double chiSquareValue, double expectedValue)
    {
        // If chi-square is significantly higher than expected, data may not be random
        // Allow 50% margin for natural variation
        return chiSquareValue <= expectedValue * 1.5;
    }

    private string GenerateAnalysisNotes(NumberFrequency[] frequencies)
    {
        var notes = new System.Text.StringBuilder();

        // Find most and least frequent numbers
        var mostFrequent = frequencies.OrderByDescending(f => f.Count).First();
        var leastFrequent = frequencies.OrderBy(f => f.Count).First();

        notes.AppendLine($"Most frequent number: {mostFrequent.Number} ({mostFrequent.Count} times, {mostFrequent.Percentage:F2}%)");
        notes.AppendLine($"Least frequent number: {leastFrequent.Number} ({leastFrequent.Count} times, {leastFrequent.Percentage:F2}%)");

        // Find numbers not yet drawn
        var neverDrawn = frequencies.Where(f => f.Count == 0).ToList();
        if (neverDrawn.Any())
        {
            notes.AppendLine($"Numbers never drawn: {string.Join(", ", neverDrawn.Select(f => f.Number))}");
        }

        // Find hot numbers (drawn in last 30 days)
        var today = DateTime.UtcNow;
        var hotNumbers = frequencies
            .Where(f => f.LastDrawnDate != DateTime.MinValue && (today - f.LastDrawnDate).TotalDays <= 30)
            .OrderByDescending(f => f.Count)
            .Take(5)
            .ToList();

        if (hotNumbers.Any())
        {
            notes.AppendLine($"Hot numbers (last 30 days): {string.Join(", ", hotNumbers.Select(f => f.Number))}");
        }

        // Find cold numbers (not drawn in last 6 months)
        var coldNumbers = frequencies
            .Where(f => f.DaysSinceLastDraw > 180)
            .OrderByDescending(f => f.DaysSinceLastDraw)
            .Take(5)
            .ToList();

        if (coldNumbers.Any())
        {
            notes.AppendLine($"Cold numbers (not drawn in 6+ months): {string.Join(", ", coldNumbers.Select(f => f.Number))}");
        }

        return notes.ToString();
    }

    private double GetChiSquareCriticalValue(int degreesOfFreedom)
    {
        // Critical values for chi-square distribution at 0.05 significance level
        // This is a simplified table - for production, use a proper chi-square calculator
        var criticalValues = new Dictionary<int, double>
        {
            { 1, 3.841 },
            { 5, 11.071 },
            { 10, 18.307 },
            { 20, 31.410 },
            { 30, 43.773 },
            { 48, 65.171 }  // 49 - 1 degrees of freedom
        };

        if (criticalValues.TryGetValue(degreesOfFreedom, out var value))
            return value;

        // Approximate for other values using Pearson's approximation
        // For large df: critical value ≈ df + sqrt(2*df) * 1.645 (for 0.05 level)
        return degreesOfFreedom + Math.Sqrt(2 * degreesOfFreedom) * 1.645;
    }
}
