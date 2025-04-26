using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoGPT.Core.Interfaces
{
    /// <summary>
    /// Interface for LLM service
    /// Equivalent to the Python OllamaClient class
    /// </summary>
    public interface ILlmService
    {
        /// <summary>
        /// Generate a response from the LLM
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="modelName">Model name to use</param>
        /// <param name="systemPrompt">System prompt for context</param>
        /// <param name="temperature">Temperature for generation (0.0 to 1.0)</param>
        /// <returns>Generated text response</returns>
        Task<string> GenerateResponseAsync(string prompt, string modelName = "llama2", string? systemPrompt = null, float temperature = 0.7f);
        
        /// <summary>
        /// Generate structured data from the LLM
        /// </summary>
        /// <typeparam name="T">Type to deserialize the JSON response to</typeparam>
        /// <param name="prompt">Input prompt</param>
        /// <param name="modelName">Model name to use</param>
        /// <param name="systemPrompt">System prompt for context</param>
        /// <param name="temperature">Temperature for generation (0.0 to 1.0)</param>
        /// <returns>Structured data object</returns>
        Task<T> GenerateStructuredResponseAsync<T>(string prompt, string modelName = "llama2", string? systemPrompt = null, float temperature = 0.7f) where T : class;
        
        /// <summary>
        /// Get a list of available models
        /// </summary>
        /// <returns>List of available model names</returns>
        Task<List<string>> GetAvailableModelsAsync();
        
        /// <summary>
        /// Check if the LLM service is connected and functioning
        /// </summary>
        /// <returns>True if LLM service is available</returns>
        Task<bool> IsHealthyAsync();
    }
}