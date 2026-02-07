using LotteryTracker.API.Data;
using LotteryTracker.API.Models;
using LotteryTracker.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LotteryTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DrawingsController : ControllerBase
{
    private readonly LotteryDbContext _context;
    private readonly ILotteryScraper _scraper;
    private readonly ILogger<DrawingsController> _logger;

    public DrawingsController(LotteryDbContext context, ILotteryScraper scraper, ILogger<DrawingsController> logger)
    {
        _context = context;
        _scraper = scraper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<LotteryDrawing>>> GetAllDrawings([FromQuery] int limit = 100)
    {
        try
        {
            var drawings = await _context.Drawings
                .OrderByDescending(d => d.DrawDate)
                .Take(limit)
                .ToListAsync();

            return Ok(drawings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving drawings");
            return StatusCode(500, new { message = "Error retrieving drawings", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LotteryDrawing>> GetDrawingById(Guid id)
    {
        try
        {
            var drawing = await _context.Drawings.FirstOrDefaultAsync(d => d.Id == id);

            if (drawing == null)
                return NotFound(new { message = "Drawing not found" });

            return Ok(drawing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving drawing {Id}", id);
            return StatusCode(500, new { message = "Error retrieving drawing", error = ex.Message });
        }
    }

    [HttpGet("latest")]
    public async Task<ActionResult<LotteryDrawing>> GetLatestDrawing()
    {
        try
        {
            var drawing = await _context.Drawings
                .OrderByDescending(d => d.DrawDate)
                .FirstOrDefaultAsync();

            if (drawing == null)
                return NotFound(new { message = "No drawings found" });

            return Ok(drawing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest drawing");
            return StatusCode(500, new { message = "Error retrieving latest drawing", error = ex.Message });
        }
    }

    [HttpPost("scrape-latest")]
    public async Task<ActionResult<LotteryDrawing?>> ScrapeLatestDrawing()
    {
        try
        {
            _logger.LogInformation("Scraping latest lottery drawing");
            var drawing = await _scraper.ScrapeLatestDrawingAsync();

            if (drawing == null)
                return StatusCode(500, new { message = "Failed to scrape latest drawing" });

            return Ok(drawing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping latest drawing");
            return StatusCode(500, new { message = "Error scraping drawing", error = ex.Message });
        }
    }

    [HttpPost("scrape-all")]
    public async Task<ActionResult<IEnumerable<LotteryDrawing>>> ScrapeAllDrawings()
    {
        try
        {
            _logger.LogInformation("Scraping all lottery drawings");
            var drawings = await _scraper.ScrapeAllDrawingsAsync();

            return Ok(drawings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping all drawings");
            return StatusCode(500, new { message = "Error scraping drawings", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDrawing(Guid id)
    {
        try
        {
            var drawing = await _context.Drawings.FindAsync(id);

            if (drawing == null)
                return NotFound(new { message = "Drawing not found" });

            _context.Drawings.Remove(drawing);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting drawing {Id}", id);
            return StatusCode(500, new { message = "Error deleting drawing", error = ex.Message });
        }
    }
}
