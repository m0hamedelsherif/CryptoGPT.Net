using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGPT.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// Implementation of ILlmService using Ollama API
    /// </summary>
    public class OllamaLlmService : ILlmService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaLlmService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // API endpoints
        private const string BaseUrl = "http://localhost:11434/api";
        private const string GenerateEndpoint = "/generate";
        private const string ModelsListEndpoint = "/tags";

        public OllamaLlmService(HttpClient httpClient, ILogger<OllamaLlmService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoGPT.Net");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<string> GetCompletionAsync(string prompt)
        {
            return await GenerateResponseAsync(prompt);
        }

        public async Task<string> GetCompletionWithContextAsync(string prompt, string context)
        {
            var systemPrompt = string.IsNullOrWhiteSpace(context) ? null : context;
            return await GenerateResponseAsync(prompt, systemPrompt: systemPrompt);
        }

        public async Task<string> GenerateMarketInsightsAsync(string marketData, string technicalAnalysis, List<string> newsData)
        {
            var prompt = $"Analyze the following crypto market data, technical analysis, and news. Provide a concise summary and actionable insights.\n\nMarket Data:\n{marketData}\n\nTechnical Analysis:\n{technicalAnalysis}\n\nNews:\n{string.Join("\n", newsData)}";
            return await GetCompletionAsync(prompt);
        }

        public async Task<string> GenerateContentAsync(string prompt)
        {
            return await GenerateResponseAsync(prompt);
        }

        public async Task<List<string>> GetAvailableModelsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(ModelsListEndpoint);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<OllamaModelsResponse>(responseString, _jsonOptions);
                if (responseData == null || responseData.Models == null)
                {
                    _logger.LogWarning("Failed to deserialize Ollama models response");
                    return new List<string>();
                }
                return responseData.Models.Select(m => m.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available models: {Message}", ex.Message);
                return new List<string>();
            }
        }

        public string GetCurrentModelName() => "llama2";
        public void SetModel(string modelName) { /* Not implemented for Ollama */ }

        private async Task<string> GenerateResponseAsync(string prompt, string modelName = "llama2", string? systemPrompt = null, float temperature = 0.7f)
        {
            try
            {
                var requestData = new OllamaRequest
                {
                    Model = modelName,
                    Prompt = prompt,
                    System = systemPrompt,
                    Temperature = temperature,
                    Stream = false
                };
                var jsonContent = JsonSerializer.Serialize(requestData, _jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                _logger.LogInformation("Sending prompt to Ollama using model {Model}", modelName);
                var response = await _httpClient.PostAsync(GenerateEndpoint, content);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<OllamaResponse>(responseString, _jsonOptions);
                if (responseData == null)
                {
                    _logger.LogWarning("Failed to deserialize Ollama response");
                    return string.Empty;
                }
                _logger.LogInformation("Received response from Ollama (took {Duration}ms)", responseData.TotalDuration);
                return responseData.Response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating LLM response: {Message}", ex.Message);
                return $"Error: {ex.Message}";
            }
        }

        #region DTO Models for Ollama API
        private class OllamaRequest
        {
            public string Model { get; set; } = string.Empty;
            public string Prompt { get; set; } = string.Empty;
            public string? System { get; set; }
            public float Temperature { get; set; }
            public bool Stream { get; set; }
        }
        private class OllamaResponse
        {
            public string Model { get; set; } = string.Empty;
            public string Response { get; set; } = string.Empty;
            public long TotalDuration { get; set; }
            public long LoadDuration { get; set; }
            public long PromptEvalCount { get; set; }
            public long PromptEvalDuration { get; set; }
            public long EvalCount { get; set; }
            public long EvalDuration { get; set; }
        }
        private class OllamaModel
        {
            public string Name { get; set; } = string.Empty;
            public string Modified { get; set; } = string.Empty;
            public long Size { get; set; }
            public string Digest { get; set; } = string.Empty;
        }
        private class OllamaModelsResponse
        {
            public List<OllamaModel> Models { get; set; } = new List<OllamaModel>();
        }
        #endregion
    }
}