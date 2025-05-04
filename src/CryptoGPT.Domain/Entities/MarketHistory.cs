using System;
using System.Collections.Generic;

namespace CryptoGPT.Domain.Entities
{
    /// <summary>
    /// Represents historical market data for a cryptocurrency
    /// </summary>
    public class MarketHistory
    {
        /// <summary>
        /// Coin identifier
        /// </summary>
        public string CoinId { get; set; } = string.Empty;
        
        /// <summary>
        /// Coin symbol/ticker
        /// </summary>
        public string Symbol { get; set; } = string.Empty;
        
        /// <summary>
        /// Historical price data points
        /// </summary>
        public List<PriceHistoryPoint> Prices { get; set; } = new List<PriceHistoryPoint>();
        
        /// <summary>
        /// Historical market cap data points
        /// </summary>
        public List<PriceHistoryPoint> MarketCaps { get; set; } = new List<PriceHistoryPoint>();
        
        /// <summary>
        /// Historical volume data points
        /// </summary>
        public List<PriceHistoryPoint> Volumes { get; set; } = new List<PriceHistoryPoint>();
        
        /// <summary>
        /// Technical indicator series data
        /// </summary>
        public Dictionary<string, List<IndicatorTimePoint>> IndicatorSeries { get; set; } = new Dictionary<string, List<IndicatorTimePoint>>();
    }
}