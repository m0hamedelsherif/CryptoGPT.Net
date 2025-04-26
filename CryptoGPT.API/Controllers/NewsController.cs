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
    public class NewsController : ControllerBase
    {
        private readonly INewsService _newsService;
        private readonly ILogger<NewsController> _logger;

        public NewsController(
            INewsService newsService,
            ILogger<NewsController> logger)
        {
            _newsService = newsService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<CryptoNewsItem>), 200)]
        public async Task<IActionResult> GetMarketNews([FromQuery] int limit = 20)
        {
            _logger.LogInformation("Getting market news (limit: {Limit})", limit);
            var news = await _newsService.GetMarketNewsAsync(limit);
            return Ok(news);
        }

        [HttpGet("{coinId}")]
        [ProducesResponseType(typeof(List<CryptoNewsItem>), 200)]
        public async Task<IActionResult> GetCoinNews(string coinId, [FromQuery] string symbol, [FromQuery] int limit = 10)
        {
            _logger.LogInformation("Getting news for {CoinId} / {Symbol} (limit: {Limit})", coinId, symbol, limit);
            
            // If symbol not provided, use coin ID as fallback
            if (string.IsNullOrEmpty(symbol))
            {
                symbol = coinId;
            }
            
            var news = await _newsService.GetCoinNewsAsync(coinId, symbol, limit);
            return Ok(news);
        }
    }
}