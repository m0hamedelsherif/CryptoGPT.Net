using CryptoGPT.Application.Common.Models;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace CryptoGPT.API.Tests
{
    public class CoinEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CoinEndpointsTests(WebApplicationFactory<Program> factory)
        {
            // Replace the real service with a mock
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real service
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(ICryptoDataService));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Create a mock service
                    var mockService = new Mock<ICryptoDataService>();

                    // Setup mock data for top coins endpoint
                    mockService.Setup(s => s.GetTopCoinsAsync(It.IsAny<int>()))
                        .ReturnsAsync(new List<CryptoCurrency>
                        {
                            new CryptoCurrency
                            {
                                Id = "bitcoin",
                                Name = "Bitcoin",
                                Symbol = "BTC",
                                CurrentPrice = 50000
                            },
                            new CryptoCurrency
                            {
                                Id = "ethereum",
                                Name = "Ethereum",
                                Symbol = "ETH",
                                CurrentPrice = 3000
                            }
                        });

                    // Setup mock data for coin details endpoint
                    mockService.Setup(s => s.GetCoinDataAsync("bitcoin"))
                        .ReturnsAsync(new CryptoCurrencyDetail
                        {
                            Id = "bitcoin",
                            Name = "Bitcoin",
                            Symbol = "BTC",
                            CurrentPrice = 50000,
                            Description = "Bitcoin is a decentralized digital currency."
                        });

                    mockService.Setup(s => s.GetCoinDataAsync("nonexistent"))
                        .ReturnsAsync((CryptoCurrencyDetail)null);

                    // Register the mock
                    services.AddScoped<ICryptoDataService>(_ => mockService.Object);
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetTopCoins_ReturnsSuccessAndCorrectContentType()
        {
            // Act
            var response = await _client.GetAsync("/api/coin?limit=10");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        }

        [Fact]
        public async Task GetTopCoins_ReturnsExpectedCoins()
        {
            // Act
            var coins = await _client.GetFromJsonAsync<List<CryptoCurrencyDto>>("/api/coin");

            // Assert
            Assert.NotNull(coins);
            Assert.Equal(2, coins.Count);
            Assert.Equal("bitcoin", coins[0].Id);
            Assert.Equal("ethereum", coins[1].Id);
        }

        [Fact]
        public async Task GetCoinDetails_WithValidId_ReturnsDetails()
        {
            // Act
            var coin = await _client.GetFromJsonAsync<CryptoCurrencyDetailDto>("/api/coin/bitcoin");

            // Assert
            Assert.NotNull(coin);
            Assert.Equal("bitcoin", coin.Id);
            Assert.Equal("Bitcoin", coin.Name);
            Assert.Equal("BTC", coin.Symbol);
            Assert.Equal(50000, coin.CurrentPrice);
        }

        [Fact]
        public async Task GetCoinDetails_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var response = await _client.GetAsync("/api/coin/nonexistent");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}