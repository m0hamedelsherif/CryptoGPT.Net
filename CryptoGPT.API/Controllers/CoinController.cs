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