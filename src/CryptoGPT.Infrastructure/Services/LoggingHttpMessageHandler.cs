using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoGPT.Infrastructure.Services
{
    /// <summary>
    /// HTTP handler that logs outgoing requests and incoming responses
    /// </summary>
    public class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHttpMessageHandler> _logger;

        public LoggingHttpMessageHandler(ILogger<LoggingHttpMessageHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[HTTP OUT] {Method} {Uri}", request.Method, request.RequestUri);
            
            var response = await base.SendAsync(request, cancellationToken);
            
            _logger.LogInformation("[HTTP IN] {StatusCode} {Uri}", 
                (int)response.StatusCode, response.RequestMessage?.RequestUri);
            
            // Log response body for debugging (only in development environment)
            if (_logger.IsEnabled(LogLevel.Debug) && response.Content != null)
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogDebug("[HTTP IN] ResponseBody: {Body}", responseBody);
                
                // Create a new content to avoid the content being read only once
                var originalContent = response.Content;
                var contentCopy = new StringContent(
                    responseBody,
                    System.Text.Encoding.UTF8,
                    originalContent.Headers.ContentType?.MediaType ?? "application/json");
                
                // Copy the headers
                foreach (var header in originalContent.Headers)
                {
                    if (!contentCopy.Headers.Contains(header.Key))
                    {
                        contentCopy.Headers.Add(header.Key, header.Value);
                    }
                }
                
                response.Content = contentCopy;
            }
            
            return response;
        }
    }
}