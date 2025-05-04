using System;
using System.Collections.Generic;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Parameters for technical indicator calculation
    /// </summary>
    public class IndicatorParameters
    {
        /// <summary>
        /// Type of indicator (e.g., "sma", "ema", "rsi", "macd", "bollinger")
        /// </summary>
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// Period for the indicator calculation (e.g., 14 for RSI)
        /// </summary>
        public int Period { get; set; } = 14;
        
        /// <summary>
        /// Fast Period for MACD calculation
        /// </summary>
        public int FastPeriod { get; set; } = 12;
        
        /// <summary>
        /// Slow Period for MACD calculation
        /// </summary>
        public int SlowPeriod { get; set; } = 26;
        
        /// <summary>
        /// Deviation for Bollinger Bands calculation
        /// </summary>
        public double Deviation { get; set; } = 2.0;
        
        /// <summary>
        /// Additional parameters specific to the indicator type
        /// </summary>
        public Dictionary<string, string> AdditionalParameters { get; set; } = new();

        /// <summary>
        /// Calculate the required number of data points for a set of indicators
        /// </summary>
        /// <param name="indicators">Dictionary of indicators to calculate</param>
        /// <param name="displayDays">Number of days to display</param>
        /// <returns>Required number of days of data to retrieve</returns>
        public static int CalculateRequiredDataPoints(Dictionary<string, IndicatorParameters> indicators, int displayDays)
        {
            var maxPeriod = displayDays;
            
            foreach (var (_, parameters) in indicators)
            {
                var requiredPoints = parameters.Type.ToLowerInvariant() switch
                {
                    "sma" => parameters.Period,
                    "ema" => parameters.Period,
                    "rsi" => parameters.Period + 1,
                    "macd" => 26 + 9, // MACD slow period + signal period
                    "bollinger" => parameters.Period,
                    _ => 0
                };
                
                maxPeriod = Math.Max(maxPeriod, displayDays + requiredPoints);
            }
            
            return maxPeriod;
        }
    }
}