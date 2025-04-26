using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Core.Models;

namespace CryptoGPT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CoinController : ControllerBase
    {
        private readonly ICryptoDataService _cryptoDataService;
        private readonly ITechnicalAnalysisService _technicalAnalysisService;
        private readonly ILogger<CoinController> _logger;

        public CoinController(
            ICryptoDataService cryptoDataService,
            ITechnicalAnalysisService technicalAnalysisService,
            ILogger<CoinController> logger)
        {
            _cryptoDataService = cryptoDataService;
            _technicalAnalysisService = technicalAnalysisService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CryptoCurrency>), 200)]
        public async Task<IActionResult> GetTopCoins([FromQuery] int limit = 10)
        {
            _logger.LogInformation("Getting top {Limit} coins", limit);
            var coins = await _cryptoDataService.GetTopCoinsAsync(limit);
            return Ok(coins);
        }

        [HttpGet("{coinId}")]
        [ProducesResponseType(typeof(CryptoCurrencyDetail), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetCoinData(string coinId)
        {
            _logger.LogInformation("Getting coin data for {CoinId}", coinId);
            var coin = await _cryptoDataService.GetCoinDataAsync(coinId);
            
            if (coin == null)
            {
                return NotFound(new { message = $"Coin {coinId} not found" });
            }
            
            return Ok(coin);
        }

        [HttpGet("{coinId}/chart")]
        [ProducesResponseType(typeof(MarketHistory), 200)]
        public async Task<IActionResult> GetMarketChart(string coinId, [FromQuery] int days = 30)
        {
            _logger.LogInformation("Getting market chart for {CoinId} ({Days} days)", coinId, days);
            var marketHistory = await _cryptoDataService.GetMarketChartAsync(coinId, days);
            return Ok(marketHistory);
        }

        [HttpGet("{coinId}/enhanced-chart")]
        [ProducesResponseType(typeof(MarketHistory), 200)]
        public async Task<IActionResult> GetEnhancedChart(
            string coinId, 
            [FromQuery] int days = 30,
            [FromQuery] string? indicators = null)
        {
            _logger.LogInformation("Getting enhanced chart for {CoinId} with {Days} days and indicators: {Indicators}", 
                coinId, days, indicators ?? "default");
            
            // Parse indicator parameters
            Dictionary<string, IndicatorParameters>? indicatorParams = null;
            
            if (!string.IsNullOrEmpty(indicators))
            {
                indicatorParams = new Dictionary<string, IndicatorParameters>();
                var indicatorList = indicators.Split(',');
                
                foreach (var indicator in indicatorList)
                {
                    // Parse indicator with parameters, format: type:period or type
                    var parts = indicator.Trim().Split(':');
                    string indicatorType = parts[0].ToLowerInvariant();
                    int period = (parts.Length > 1 && int.TryParse(parts[1], out int p)) ? p : 0;
                    
                    switch (indicatorType)
                    {
                        case "rsi":
                            indicatorParams[$"rsi_{period}"] = IndicatorParameters.RSI(
                                period > 0 ? period : 14);
                            break;
                            
                        case "sma":
                            indicatorParams[$"sma_{period}"] = IndicatorParameters.SMA(
                                period > 0 ? period : 20);
                            break;
                            
                        case "ema":
                            indicatorParams[$"ema_{period}"] = IndicatorParameters.EMA(
                                period > 0 ? period : 20);
                            break;
                            
                        case "macd":
                            // MACD can have custom parameters: macd:12:26:9 (fast:slow:signal)
                            int fastPeriod = 12;
                            int slowPeriod = 26;
                            int signalPeriod = 9;
                            
                            if (parts.Length > 1 && int.TryParse(parts[1], out int fast))
                                fastPeriod = fast;
                                
                            if (parts.Length > 2 && int.TryParse(parts[2], out int slow))
                                slowPeriod = slow;
                                
                            if (parts.Length > 3 && int.TryParse(parts[3], out int signal))
                                signalPeriod = signal;
                                
                            indicatorParams["macd"] = IndicatorParameters.MACD(
                                fastPeriod, slowPeriod, signalPeriod);
                            break;
                            
                        case "bb":
                        case "bollinger":
                            // Bollinger can have custom parameters: bollinger:20:2 (period:stdDev)
                            double stdDev = 2.0;
                            if (parts.Length > 2 && double.TryParse(parts[2], out double sd))
                                stdDev = sd;
                                
                            indicatorParams["bollinger"] = IndicatorParameters.BollingerBands(
                                period > 0 ? period : 20, stdDev);
                            break;
                            
                        default:
                            _logger.LogWarning("Unknown indicator type: {IndicatorType}", indicatorType);
                            break;
                    }
                }
            }
            
            // Get extended chart with indicators
            var marketHistory = await _cryptoDataService.GetExtendedMarketChartAsync(coinId, days, indicatorParams);
            
            // Check if error indicator is present
            if (marketHistory.IndicatorSeries != null && 
                marketHistory.IndicatorSeries.ContainsKey("error"))
            {
                return Problem(
                    detail: "Failed to load enough historical data for the requested indicators.",
                    title: "Insufficient Historical Data",
                    statusCode: 422
                );
            }
            
            return Ok(marketHistory);
        }

        [HttpGet("overview")]
        [ProducesResponseType(typeof(MarketOverview), 200)]
        public async Task<IActionResult> GetMarketOverview()
        {
            _logger.LogInformation("Getting market overview");
            var overview = await _cryptoDataService.GetMarketOverviewAsync();
            return Ok(overview);
        }

        [HttpGet("{symbol}/technical-analysis")]
        [ProducesResponseType(typeof(TechnicalAnalysis), 200)]
        public async Task<IActionResult> GetTechnicalAnalysis(string symbol)
        {
            _logger.LogInformation("Getting technical analysis for {Symbol}", symbol);
            var analysis = await _technicalAnalysisService.AnalyzeCryptoAsync(symbol);
            return Ok(analysis);
        }

        [HttpGet("technical-indicators")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public async Task<IActionResult> GetAvailableIndicators()
        {
            _logger.LogInformation("Getting available technical indicators");
            var indicators = await _technicalAnalysisService.GetAvailableIndicatorsAsync();
            return Ok(indicators);
        }

        [HttpGet("source")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetDataSource()
        {
            string source = _cryptoDataService.GetCurrentDataSource();
            return Ok(new { source });
        }
    }
}