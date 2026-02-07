using LotteryTracker.API.Data;
using LotteryTracker.API.Models;
using LotteryTracker.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotteryTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IStatisticalAnalysisService _analysisService;
    private readonly LotteryDbContext _context;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IStatisticalAnalysisService analysisService, LotteryDbContext context, ILogger<AnalyticsController> logger)
    {
        _analysisService = analysisService;
        _context = context;
        _logger = logger;
    }

    [HttpGet("frequencies")]
    public ActionResult<NumberFrequency[]> GetNumberFrequencies()
    {
        try
        {
            Console.WriteLine("Calculating number frequencies...");
            var frequencies = _analysisService.CalculateNumberFrequencies();
            return Ok(frequencies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating number frequencies");
            return StatusCode(500, new { message = "Error calculating frequencies", error = ex.Message });
        }
    }

    [HttpGet("frequencies/hot")]
    public ActionResult<IEnumerable<NumberFrequency>> GetHotNumbers([FromQuery] int count = 10)
    {
        try
        {
            var frequencies = _analysisService.CalculateNumberFrequencies();
            var hotNumbers = frequencies
                .OrderByDescending(f => f.Count)
                .Take(count)
                .ToList();

            return Ok(hotNumbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hot numbers");
            return StatusCode(500, new { message = "Error retrieving hot numbers", error = ex.Message });
        }
    }

    [HttpGet("frequencies/cold")]
    public ActionResult<IEnumerable<NumberFrequency>> GetColdNumbers([FromQuery] int count = 10)
    {
        try
        {
            var frequencies = _analysisService.CalculateNumberFrequencies();
            var coldNumbers = frequencies
                .OrderBy(f => f.Count)
                .Take(count)
                .ToList();

            return Ok(coldNumbers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cold numbers");
            return StatusCode(500, new { message = "Error retrieving cold numbers", error = ex.Message });
        }
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<StatisticalAnalysis>> PerformAnalysis()
    {
        try
        {
            _logger.LogInformation("Performing statistical analysis");
            var analysis = await _analysisService.AnalyzeDrawingsAsync();

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing statistical analysis");
            return StatusCode(500, new { message = "Error performing analysis", error = ex.Message });
        }
    }

    [HttpGet("analysis/latest")]
    public async Task<ActionResult<StatisticalAnalysis>> GetLatestAnalysis()
    {
        try
        {
            var analysis = await _context.Analyses
                .OrderByDescending(a => a.AnalyzedDate)
                .FirstOrDefaultAsync();

            if (analysis == null)
                return NotFound(new { message = "No analysis found. Run /api/analytics/analyze first." });

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest analysis");
            return StatusCode(500, new { message = "Error retrieving analysis", error = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetAnalyticsSummary()
    {
        try
        {
            var totalDrawings = await _context.Drawings.CountAsync();
            var frequencies = _analysisService.CalculateNumberFrequencies();
            var latestAnalysis = await _context.Analyses
                .OrderByDescending(a => a.AnalyzedDate)
                .FirstOrDefaultAsync();

            var summary = new
            {
                totalDrawings,
                mostFrequentNumber = frequencies.OrderByDescending(f => f.Count).First(),
                leastFrequentNumber = frequencies.OrderBy(f => f.Count).First(),
                numberNeverDrawn = frequencies.Where(f => f.Count == 0).Select(f => f.Number).ToList(),
                hasLatestAnalysis = latestAnalysis != null,
                lastAnalysisDate = latestAnalysis?.AnalyzedDate,
                isApparentlyRandomized = latestAnalysis?.IsRandomized ?? false
            };

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving analytics summary");
            return StatusCode(500, new { message = "Error retrieving summary", error = ex.Message });
        }
    }
}
