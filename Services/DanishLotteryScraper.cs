using LotteryTracker.API.Data;
using LotteryTracker.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LotteryTracker.API.Services;

public interface ILotteryScraper
{
    Task<LotteryDrawing?> ScrapeLatestDrawingAsync();
    Task<List<LotteryDrawing>> ScrapeAllDrawingsAsync();
}

public class DanishLotteryScraper : ILotteryScraper
{
    private readonly ILogger<DanishLotteryScraper> _logger;
    private readonly HttpClient _httpClient;
    private readonly LotteryDbContext _context;

    private const string LOTTERY_API_URL = "https://danskespil.dk/dlo/scapi/danskespil/numbergames/lotto/winningNumbers";
    private const int TIMEOUT_SECONDS = 30;

    // DTO classes for JSON response
    private class LottoApiResponse
    {
        [JsonPropertyName("lottoSaturday")]
        public LottoDrawData? LottoSaturday { get; set; }

        [JsonPropertyName("lottoWednesday")]
        public LottoDrawData? LottoWednesday { get; set; }
    }

    private class LottoDrawData
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("winningNumbers")]
        public int[]? WinningNumbers { get; set; }

        [JsonPropertyName("bonusNumber")]
        public int? BonusNumber { get; set; }
    }

    public DanishLotteryScraper(ILogger<DanishLotteryScraper> logger, HttpClient httpClient, LotteryDbContext context)
    {
        _logger = logger;
        _httpClient = httpClient;
        _context = context;
    }

    public async Task<LotteryDrawing?> ScrapeLatestDrawingAsync()
    {
        try
        {
            _logger.LogInformation("Starting to scrape latest lottery drawing from API");

            // Calculate the most recent Saturday (draws at 21:00 CET)
            var now = DateTime.Now;

            // Start from today and go backwards to find a Saturday
            var checkDate = now.Date;

            // If today is Saturday and before 21:00, start checking from yesterday
            if (checkDate.DayOfWeek == DayOfWeek.Saturday && now.Hour < 21)
            {
                checkDate = checkDate.AddDays(-1);
            }

            // Find the most recent Saturday
            while (checkDate.DayOfWeek != DayOfWeek.Saturday)
            {
                checkDate = checkDate.AddDays(-1);
            }

            var dateParam = checkDate.ToString("yyyy-MM-dd");
            _logger.LogInformation("Fetching lottery draw for Saturday: {Date} (DayOfWeek: {DayOfWeek})",
                dateParam, checkDate.DayOfWeek);

            var drawing = await FetchDrawingFromApiAsync(dateParam);

            if (drawing != null)
            {
                // Check if drawing already exists in database
                var existing = await _context.Drawings
                    .FirstOrDefaultAsync(d => d.DrawDate.Date == drawing.DrawDate.Date);

                if (existing == null)
                {
                    _context.Drawings.Add(drawing);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved new lottery drawing for {DrawDate} with numbers: {Numbers}",
                        drawing.DrawDate, string.Join(", ", drawing.WinningNumbers));
                }
                else
                {
                    _logger.LogInformation("Drawing for {DrawDate} already exists in database", drawing.DrawDate);
                    return existing;
                }
            }
            else
            {
                _logger.LogWarning("No lottery draw found for {Date}", dateParam);
            }

            return drawing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping latest lottery drawing");
            return null;
        }
    }

    public async Task<List<LotteryDrawing>> ScrapeAllDrawingsAsync()
    {
        var drawings = new List<LotteryDrawing>();

        try
        {
            _logger.LogInformation("Starting to scrape historical lottery drawings");

            // Scrape last 52 Saturdays (1 year of weekly draws)
            var today = DateTime.Now;
            var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
            var lastSaturday = today.AddDays(-daysUntilSaturday);

            if (daysUntilSaturday == 0 && today.Hour < 21)
            {
                lastSaturday = lastSaturday.AddDays(-7); // If today is Saturday before 21:00, start from previous Saturday
            }

            for (int week = 0; week < 52; week++)
            {
                var saturday = lastSaturday.AddDays(-7 * week);
                var dateParam = saturday.ToString("yyyy-MM-dd");
                var drawing = await FetchDrawingFromApiAsync(dateParam);

                if (drawing != null)
                {
                    var existing = await _context.Drawings
                        .FirstOrDefaultAsync(d => d.DrawDate.Date == drawing.DrawDate.Date);

                    if (existing == null)
                    {
                        _context.Drawings.Add(drawing);
                        drawings.Add(drawing);
                    }
                }

                // Be nice to the API
                await Task.Delay(100);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} new lottery drawings", drawings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping all lottery drawings");
        }

        return drawings;
    }

    private async Task<LotteryDrawing?> FetchDrawingFromApiAsync(string dateParam)
    {
        try
        {
            var url = $"{LOTTERY_API_URL}?date={dateParam}";

            _logger.LogInformation("Fetching lottery data from: {Url}", url);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TIMEOUT_SECONDS));
            var response = await _httpClient.GetAsync(url, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API returned status code {StatusCode} for URL: {Url}",
                    response.StatusCode, url);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Received JSON response ({Length} chars): {JsonPreview}",
                json.Length,
                json.Substring(0, Math.Min(200, json.Length)));

            var apiResponse = JsonSerializer.Deserialize<LottoApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogInformation("Deserialized response - LottoSaturday: {HasSat}, LottoWednesday: {HasWed}",
                apiResponse?.LottoSaturday != null,
                apiResponse?.LottoWednesday != null);

            // Try Saturday draw first, then Wednesday
            var drawData = apiResponse?.LottoSaturday ?? apiResponse?.LottoWednesday;

            if (drawData?.WinningNumbers == null || drawData.WinningNumbers.Length < 7)
            {
                _logger.LogWarning("No valid draw data found in response. DrawData null: {IsNull}, WinningNumbers: {Numbers}",
                    drawData == null,
                    drawData?.WinningNumbers?.Length ?? 0);
                return null;
            }

            if (!DateTime.TryParse(drawData.Date, out var drawDate))
            {
                _logger.LogWarning("Failed to parse date: {Date}", drawData.Date);
                return null;
            }

            // Danish Lotto uses numbers 1-36, validate and take first 7
            var validNumbers = drawData.WinningNumbers
                .Where(n => n >= 1 && n <= 36)
                .Take(7)
                .ToArray();

            if (validNumbers.Length < 7)
            {
                _logger.LogWarning("Invalid number of valid winning numbers (1-36): {Count}/7", validNumbers.Length);
                return null;
            }

            var drawing = new LotteryDrawing
            {
                DrawDate = drawDate,
                WinningNumbers = validNumbers
            };

            _logger.LogInformation("Successfully parsed drawing for {Date}: {Numbers}",
                drawDate.ToString("yyyy-MM-dd"),
                string.Join(", ", drawing.WinningNumbers));

            return drawing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching drawing data for date {Date}", dateParam);
            return null;
        }
    }
}
