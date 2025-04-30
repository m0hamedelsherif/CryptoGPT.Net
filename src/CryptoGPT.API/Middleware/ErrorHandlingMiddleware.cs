using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CryptoGPT.API.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var result = string.Empty;

            switch (exception)
            {
                case ValidationException validationException:
                    code = HttpStatusCode.BadRequest;
                    result = JsonSerializer.Serialize(new
                    {
                        title = "Validation Error",
                        status = (int)code,
                        errors = validationException.Errors
                    });
                    break;

                case KeyNotFoundException _:
                    code = HttpStatusCode.NotFound;
                    result = JsonSerializer.Serialize(new
                    {
                        title = "Resource Not Found",
                        status = (int)code,
                        detail = exception.Message
                    });
                    break;

                case UnauthorizedAccessException _:
                    code = HttpStatusCode.Unauthorized;
                    result = JsonSerializer.Serialize(new
                    {
                        title = "Unauthorized",
                        status = (int)code,
                        detail = exception.Message
                    });
                    break;

                default:
                    _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
                    result = JsonSerializer.Serialize(new
                    {
                        title = "Server Error",
                        status = (int)code,
                        detail = "An internal server error has occurred."
                    });
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            await context.Response.WriteAsync(result);
        }
    }
}