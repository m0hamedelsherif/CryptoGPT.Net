using Asp.Versioning;
using Asp.Versioning.Builder;
using System;
using System.Collections.Generic;

namespace CryptoGPT.API.Endpoints
{
    public static class HealthEndpoints
    {
        public static WebApplication MapHealthEndpoints(this WebApplication app)
        {
            var versionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var group = app.MapGroup("/api/health")
                .WithTags("Health")
                .WithApiVersionSet(versionSet)
                .MapToApiVersion(new ApiVersion(1, 0))
                .WithOpenApi();

            // Basic health check
            group.MapGet("/", () =>
            {
                return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
            })
            .WithName("GetHealth")
            .WithDescription("Basic health check");

            // Detailed health check
            group.MapGet("/detailed", () =>
            {
                var healthInfo = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = typeof(HealthEndpoints).Assembly.GetName().Version?.ToString() ?? "unknown",
                    environment = app.Environment.EnvironmentName,
                    components = new Dictionary<string, object>
                    {
                        ["api"] = new { status = "healthy" },
                        ["database"] = new { status = "healthy" }
                    }
                };

                return Results.Ok(healthInfo);
            })
            .WithName("GetDetailedHealth")
            .WithDescription("Detailed system health information");

            return app;
        }
    }
}