using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Application.Interfaces
{
    /// <summary>
    /// Interface for interacting with large language models
    /// </summary>
    public interface ILlmService
    {
        /// <summary>
        /// Get LLM response for a prompt
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM</param>
        /// <returns>Generated LLM response</returns>
        Task<string> GetCompletionAsync(string prompt);
        
        /// <summary>
        /// Get LLM response with context
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM</param>
        /// <param name="context">Additional context to provide to the LLM</param>
        /// <returns>Generated LLM response</returns>
        Task<string> GetCompletionWithContextAsync(string prompt, string context);
        
        /// <summary>
        /// Generate crypto market insights based on provided data
        /// </summary>
        /// <param name="marketData">Market data to analyze</param>
        /// <param name="technicalAnalysis">Technical analysis data</param>
        /// <param name="newsData">Recent news articles</param>
        /// <returns>Generated market insights</returns>
        Task<string> GenerateMarketInsightsAsync(
            string marketData, 
            string technicalAnalysis, 
            List<string> newsData);

        /// <summary>
        /// Generate content based on a prompt
        /// </summary>
        /// <param name="prompt">The prompt to generate content from</param>
        /// <returns>Generated content</returns>
        Task<string> GenerateContentAsync(string prompt);

        /// <summary>
        /// Get available LLM models
        /// </summary>
        /// <returns>List of available model names</returns>
        Task<List<string>> GetAvailableModelsAsync();
        
        /// <summary>
        /// Get the currently active LLM model name
        /// </summary>
        /// <returns>Current model name</returns>
        string GetCurrentModelName();
        
        /// <summary>
        /// Set the active LLM model
        /// </summary>
        /// <param name="modelName">Model name to use</param>
        void SetModel(string modelName);
    }
}