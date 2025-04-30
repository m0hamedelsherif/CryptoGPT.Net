using CryptoGPT.Application.Features.Coins.Queries.GetTopCoins;
using CryptoGPT.Application.Interfaces;
using CryptoGPT.Domain.Entities;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CryptoGPT.Application.Tests.Features.Coins.Queries.GetTopCoins
{
    public class GetTopCoinsQueryHandlerTests
    {
        private readonly Mock<ICryptoDataService> _mockCryptoDataService;
        private readonly GetTopCoinsQueryHandler _sut; // System Under Test

        public GetTopCoinsQueryHandlerTests()
        {
            _mockCryptoDataService = new Mock<ICryptoDataService>();
            _sut = new GetTopCoinsQueryHandler(_mockCryptoDataService.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnCorrectNumberOfCoins()
        {
            // Arrange
            int limit = 5;
            var query = new GetTopCoinsQuery { Limit = limit };
            
            var mockCoins = new List<CryptoCurrency>
            {
                new CryptoCurrency { Id = "bitcoin", Name = "Bitcoin", Symbol = "BTC", CurrentPrice = 50000 },
                new CryptoCurrency { Id = "ethereum", Name = "Ethereum", Symbol = "ETH", CurrentPrice = 3000 },
                new CryptoCurrency { Id = "ripple", Name = "XRP", Symbol = "XRP", CurrentPrice = 0.5m },
                new CryptoCurrency { Id = "cardano", Name = "Cardano", Symbol = "ADA", CurrentPrice = 1.2m },
                new CryptoCurrency { Id = "solana", Name = "Solana", Symbol = "SOL", CurrentPrice = 100 }
            };

            _mockCryptoDataService
                .Setup(service => service.GetTopCoinsAsync(limit))
                .ReturnsAsync(mockCoins);

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(limit, result.Count);
            Assert.Equal("bitcoin", result.First().Id);
            Assert.Equal("solana", result.Last().Id);
            
            // Verify the service was called with the correct parameter
            _mockCryptoDataService.Verify(service => service.GetTopCoinsAsync(limit), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldMapPropertiesCorrectly()
        {
            // Arrange
            var query = new GetTopCoinsQuery { Limit = 1 };
            
            var mockCoin = new CryptoCurrency 
            { 
                Id = "bitcoin", 
                Name = "Bitcoin", 
                Symbol = "BTC", 
                CurrentPrice = 50000,
                MarketCap = 1000000000000,
                PriceChangePercentage24h = 2.5m,
                MarketCapRank = 1,
                Volume24h = 30000000000,
                ImageUrl = "https://example.com/bitcoin.png"
            };

            _mockCryptoDataService
                .Setup(service => service.GetTopCoinsAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<CryptoCurrency> { mockCoin });

            // Act
            var result = await _sut.Handle(query, CancellationToken.None);
            var dto = result.First();

            // Assert
            Assert.Equal(mockCoin.Id, dto.Id);
            Assert.Equal(mockCoin.Name, dto.Name);
            Assert.Equal(mockCoin.Symbol, dto.Symbol);
            Assert.Equal(mockCoin.CurrentPrice, dto.CurrentPrice);
            Assert.Equal(mockCoin.MarketCap, dto.MarketCap);
            Assert.Equal(mockCoin.PriceChangePercentage24h, dto.PriceChangePercentage24h);
            Assert.Equal(mockCoin.MarketCapRank, dto.MarketCapRank);
            Assert.Equal(mockCoin.Volume24h, dto.Volume24h);
            Assert.Equal(mockCoin.ImageUrl, dto.ImageUrl);
        }
    }
}