using Microsoft.OpenApi.Models;
using CryptoGPT.Core.Interfaces;
using CryptoGPT.Services;
using Serilog;
using Microsoft.AspNetCore.HttpLogging;
using CryptoGPT.API;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    //.WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        //.WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .Enrich.WithProperty("ApplicationName", "CryptoGPT.Net");
});

builder.Services.AddSerilog();

builder.Services.AddHttpLogging(options =>
{
    options.LoggingFields = HttpLoggingFields.All;
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CryptoGPT.Net API",
        Version = "v1",
        Description = "API for cryptocurrency data, recommendations, and news",
        Contact = new OpenApiContact
        {
            Name = "CryptoGPT",
            Email = "contact@cryptogpt.net"
        }
    });
});

// Configure service behaviors
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add in-memory cache for HybridCacheService
builder.Services.AddMemoryCache();

// Register LoggingHttpMessageHandler for outgoing HTTP clients
builder.Services.AddTransient<LoggingHttpMessageHandler>();

// Register HTTP clients with logging handler
builder.Services.AddHttpClient<INewsService, NewsService>()
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();
builder.Services.AddHttpClient<ITechnicalAnalysisService, TechnicalAnalysisService>()
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();
builder.Services.AddHttpClient<ILlmService, OllamaLlmService>()
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();
builder.Services.AddHttpClient<ICoinGeckoService, CoinGeckoService>()
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();
builder.Services.AddHttpClient<ICoinCapService, CoinCapService>()
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();
builder.Services.AddHttpClient<IYahooFinanceService, YahooFinanceService>()
    .AddHttpMessageHandler<LoggingHttpMessageHandler>();

// Register services
builder.Services.AddSingleton<ICacheService, HybridCacheService>();
builder.Services.AddScoped<ICryptoDataService, MultiSourceCryptoService>();
builder.Services.AddScoped<ILlmService, OllamaLlmService>();
builder.Services.AddScoped<INewsService, NewsService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var cacheService = sp.GetRequiredService<ICacheService>();
    var logger = sp.GetRequiredService<ILogger<NewsService>>();

    // Get API key from configuration
    var apiKey = builder.Configuration["NewsApiKey"] ?? "";

    return new NewsService(httpClient, cacheService, logger);
});

builder.Services.AddCoinGecko();

builder.Services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();
builder.Services.AddScoped<IRecommendationEngine, RecommendationEngine>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpLogging();
    // request response logging
    app.UseSerilogRequestLogging();
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// Request/Response logging middleware
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    var requestBody = string.Empty;
    if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
    {
        context.Request.Body.Position = 0;
        using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }
    }
    Log.Information("HTTP Request: {method} {url} {body}", context.Request.Method, context.Request.Path, requestBody);
    var originalBodyStream = context.Response.Body;
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;
    await next();
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
    context.Response.Body.Seek(0, SeekOrigin.Begin);
    Log.Information("HTTP Response: {statusCode} {body}", context.Response.StatusCode, responseText);
    await responseBody.CopyToAsync(originalBodyStream);
});

app.MapControllers();

app.Run();