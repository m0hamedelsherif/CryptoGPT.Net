using CryptoGPT.Domain.Entities;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for providing cryptocurrency investment recommendations
    /// </summary>
    public interface IRecommendationEngine
    {
        /// <summary>
        /// Get a market overview with key trends and recommendations
        /// </summary>
        Task<MarketOverview> GetMarketOverviewAsync();

        /// <summary>
        /// Get investment recommendation for a specific asset
        /// </summary>
        Task<AssetRecommendation> GetAssetRecommendationAsync(string coinId);
    }
}