using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CryptoGPT.Core.Interfaces;

namespace CryptoGPT.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ICryptoDataService _cryptoDataService;
        private readonly ICacheService _cacheService;
        private readonly ILlmService _llmService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            ICryptoDataService cryptoDataService,
            ICacheService cacheService,
            ILlmService llmService,
            ILogger<HealthController> logger)
        {
            _cryptoDataService = cryptoDataService;
            _cacheService = cacheService;
            _llmService = llmService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "operational", timestamp = DateTime.UtcNow });
        }

        [HttpGet("detailed")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        public async Task<IActionResult> GetDetailedHealth()
        {
            _logger.LogInformation("Checking detailed system health");
            
            bool cacheStatus = _cacheService.IsConnected();
            bool llmStatus = await _llmService.IsHealthyAsync();
            string dataSource = _cryptoDataService.GetCurrentDataSource();

            var result = new Dictionary<string, object>
            {
                { "status", "operational" },
                { "timestamp", DateTime.UtcNow },
                { "services", new Dictionary<string, object>
                    {
                        { "cache", new { status = cacheStatus ? "connected" : "disconnected" } },
                        { "llm", new { status = llmStatus ? "operational" : "unavailable" } },
                        { "data", new { status = "operational", source = dataSource } }
                    }
                }
            };
            
            return Ok(result);
        }
    }
}