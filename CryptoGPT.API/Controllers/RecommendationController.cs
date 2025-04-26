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
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationEngine _recommendationEngine;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IRecommendationEngine recommendationEngine,
            ILogger<RecommendationController> logger)
        {
            _recommendationEngine = recommendationEngine;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        public async Task<IActionResult> GenerateRecommendations([FromBody] RecommendationRequest request)
        {
            _logger.LogInformation("Generating recommendations for query: {Query}, risk profile: {RiskProfile}", 
                request.Query, request.RiskProfile);
                
            var recommendations = await _recommendationEngine.GenerateRecommendationsAsync(
                request.Query, 
                request.RiskProfile
            );
            
            return Ok(recommendations);
        }

        [HttpGet("market-snapshot")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        public async Task<IActionResult> GetMarketSnapshot()
        {
            _logger.LogInformation("Getting market snapshot");
            var snapshot = await _recommendationEngine.GetMarketSnapshotAsync();
            return Ok(snapshot);
        }
    }

    public class RecommendationRequest
    {
        public string Query { get; set; } = "Investment advice for crypto";
        public RiskProfile RiskProfile { get; set; } = RiskProfile.Moderate;
    }
}