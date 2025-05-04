using CryptoGPT.Application.Common.Models;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Features.Coins.Queries.GetTechnicalAnalysis
{
    public class GetTechnicalAnalysisQueryHandler : IRequestHandler<GetTechnicalAnalysisQuery, TechnicalAnalysisDto>
    {
        private readonly ICryptoDataService _cryptoDataService;
        private readonly ILogger<GetTechnicalAnalysisQueryHandler> _logger;

        public GetTechnicalAnalysisQueryHandler(
            ICryptoDataService cryptoDataService,
            ILogger<GetTechnicalAnalysisQueryHandler> logger)
        {
            _cryptoDataService = cryptoDataService ?? throw new ArgumentNullException(nameof(cryptoDataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TechnicalAnalysisDto> Handle(GetTechnicalAnalysisQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting technical analysis for coin {CoinId} over {Days} days", request.CoinId, request.Days);

            var analysis = await _cryptoDataService.GetTechnicalAnalysisAsync(request.CoinId, request.Days);

            // Create metadata from support/resistance levels and latest indicator values
            var metaData = new Dictionary<string, object>
            {
                { "SupportLevels", analysis.SupportLevels },
                { "ResistanceLevels", analysis.ResistanceLevels },
                { "GeneratedAt", DateTimeOffset.FromUnixTimeMilliseconds(analysis.Timestamp) },
                { "PeriodDays", analysis.PeriodDays }
            };

            // Add latest indicator values to metadata
            metaData.Add("LatestValues", analysis.LatestValues);

            // Organize moving averages indicators
            var movingAverages = new Dictionary<string, object>();
            var maGroup = analysis.IndicatorGroups.FirstOrDefault(g => g.Type == "Moving Averages");
            if (maGroup != null)
            {
                movingAverages.Add("Description", maGroup.Description);
                movingAverages.Add("Meaning", maGroup.Meaning);
                movingAverages.Add("Weight", maGroup.Weight);
                movingAverages.Add("Indicators", maGroup.Indicators);
            }

            // Organize oscillators indicators
            var oscillators = new Dictionary<string, object>();
            var oscGroup = analysis.IndicatorGroups.FirstOrDefault(g => g.Type == "Oscillators");
            if (oscGroup != null)
            {
                oscillators.Add("Description", oscGroup.Description);
                oscillators.Add("Meaning", oscGroup.Meaning);
                oscillators.Add("Weight", oscGroup.Weight);
                oscillators.Add("Indicators", oscGroup.Indicators);
            }

            // Organize volatility indicators
            var volatilityIndicators = new Dictionary<string, object>();
            var volGroup = analysis.IndicatorGroups.FirstOrDefault(g => g.Type == "Volatility Indicators");
            if (volGroup != null)
            {
                volatilityIndicators.Add("Description", volGroup.Description);
                volatilityIndicators.Add("Meaning", volGroup.Meaning);
                volatilityIndicators.Add("Weight", volGroup.Weight);
                volatilityIndicators.Add("Indicators", volGroup.Indicators);
            }

            // Add overall assessment to metadata
            metaData.Add("Overall", analysis.Overall);
            metaData.Add("SignalsList", analysis.SignalsList);

            // Map domain entity to DTO
            var result = new TechnicalAnalysisDto
            {
                CoinId = analysis.CoinId,
                Signal = analysis.Signal,
                Strength = analysis.Strength,
                Trend = analysis.Trend,
                GeneratedAt = DateTimeOffset.FromUnixTimeMilliseconds(analysis.Timestamp).UtcDateTime,

                // Indicator time series data for charting
                Indicators = analysis.Indicators,
                
                // Metadata and organized indicator groups
                MetaData = metaData,
                MovingAverages = movingAverages,
                Oscillators = oscillators,
                VolatilityIndicators = volatilityIndicators,
                
                // Signals and summary
                IndicatorSignals = analysis.Signals,
                Summary = analysis.TrendAnalysis,
                
                // Add volume indicators section if we have any
                VolumeIndicators = new Dictionary<string, object>
                {
                    { "Description", "Indicators based on trading volume" },
                    { "Meaning", "Volume indicators help confirm trend strength and potential reversals" }
                }
            };

            return result;
        }
    }
}