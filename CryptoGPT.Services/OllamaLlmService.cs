using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CryptoGPT.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.Services
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

            // Configure HttpClient
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "CryptoGPT.Net");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <inheritdoc/>
        public async Task<string> GenerateResponseAsync(string prompt, string modelName = "llama2", string? systemPrompt = null, float temperature = 0.7f)
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

        /// <inheritdoc/>
        public async Task<T> GenerateStructuredResponseAsync<T>(string prompt, string modelName = "llama2", string? systemPrompt = null, float temperature = 0.7f) where T : class
        {
            try
            {
                // Add JSON output instruction to the system prompt
                string jsonSystemPrompt = systemPrompt ?? "";
                if (!string.IsNullOrEmpty(jsonSystemPrompt))
                {
                    jsonSystemPrompt += "\n\n";
                }
                jsonSystemPrompt += "You must respond with valid JSON only, with no other text. Format your response as a valid JSON object.";

                // Get response as string
                var jsonResponse = await GenerateResponseAsync(prompt, modelName, jsonSystemPrompt, temperature);

                // Clean up the response to ensure it's valid JSON
                jsonResponse = CleanJsonResponse(jsonResponse);

                // Parse JSON to the requested type
                var result = JsonSerializer.Deserialize<T>(jsonResponse, _jsonOptions);
                
                if (result == null)
                {
                    _logger.LogWarning("Failed to parse structured response from LLM");
                    throw new JsonException("Failed to parse structured response from LLM");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating structured LLM response: {Message}", ex.Message);
                throw;
            }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                // Try to get available models as a basic health check
                var models = await GetAvailableModelsAsync();
                return models.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM service health check failed: {Message}", ex.Message);
                return false;
            }
        }

        #region Helper Methods

        private string CleanJsonResponse(string response)
        {
            // Extract JSON from a response that may contain markdown code blocks
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd >= 0 && jsonEnd > jsonStart)
            {
                return response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            // If no JSON object is found, try array
            var arrayStart = response.IndexOf('[');
            var arrayEnd = response.LastIndexOf(']');

            if (arrayStart >= 0 && arrayEnd >= 0 && arrayEnd > arrayStart)
            {
                return response.Substring(arrayStart, arrayEnd - arrayStart + 1);
            }

            return response;
        }

        #endregion

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