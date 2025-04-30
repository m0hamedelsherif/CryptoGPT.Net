using Asp.Versioning;
using CryptoGPT.API.Endpoints;
using CryptoGPT.API.Middleware;
using CryptoGPT.Application;
using CryptoGPT.Infrastructure;
using Microsoft.AspNetCore.Http.Json;
using Scalar;
using Scalar.AspNetCore;
using Serilog;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("CryptoGPT.API.Tests")]
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI with Scalar
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "CryptoGPT.Net API",
        Version = "v1",
        Description = "API for cryptocurrency data, recommendations, and news"
    });
});

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("X-Api-Version")
    );
});

// Configure JSON options
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add application layer services
builder.Services.AddApplication();

// Add infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Serves the OpenAPI/Swagger JSON document
    app.MapScalarApiReference();
    app.UseDeveloperExceptionPage();
    app.UseSerilogRequestLogging();
}
else
{
    // Use custom error handling middleware in production
    app.UseMiddleware<ErrorHandlingMiddleware>();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Map API endpoints
app.MapCoinEndpoints();
app.MapNewsEndpoints();
app.MapRecommendationEndpoints();
app.MapHealthEndpoints();

app.Run();

// This needs to be at the end of the file, after all top-level statements
public partial class Program
{ }