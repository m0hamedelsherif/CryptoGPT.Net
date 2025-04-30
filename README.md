# CryptoGPT.Net

A .NET API for cryptocurrency data, news, and AI-powered recommendations with Clean Architecture and Minimal APIs.

## Project Structure

This project follows Clean Architecture principles:

```
CryptoGPT.Net/
├── src/                           # Source code
│   ├── CryptoGPT.Domain/          # Domain entities and business rules
│   ├── CryptoGPT.Application/     # Application services, interfaces, and DTOs
│   ├── CryptoGPT.Infrastructure/  # Implementations of interfaces
│   └── CryptoGPT.API/             # API endpoints and configuration
│
├── tests/                         # Test projects
│   ├── CryptoGPT.Domain.Tests/
│   ├── CryptoGPT.Application.Tests/
│   ├── CryptoGPT.Infrastructure.Tests/
│   └── CryptoGPT.API.Tests/
│
└── CryptoGPT.Net.sln              # Solution file
```

## Features

- **Cryptocurrency Data**: Get real-time and historical cryptocurrency data from multiple sources (CoinGecko, CoinCap, etc.)
- **Technical Analysis**: Get technical indicators and charts for cryptocurrencies
- **News**: Get latest cryptocurrency news
- **Recommendations**: Get AI-powered recommendations for cryptocurrencies and portfolio optimization

## Technology Stack

- **.NET 8**: Latest .NET platform
- **Minimal APIs**: Modern, high-performance API endpoints
- **Clean Architecture**: Separation of concerns and dependency inversion
- **CQRS with MediatR**: Command Query Responsibility Segregation pattern
- **API Versioning**: Support for multiple API versions
- **FluentValidation**: Robust input validation
- **SwaggerUI/OpenAPI**: API documentation
- **xUnit**: Unit and integration testing

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022, VS Code, or JetBrains Rider

### Installation

1. Clone the repository
   ```bash
   git clone https://github.com/yourusername/CryptoGPT.Net.git
   cd CryptoGPT.Net
   ```

2. Build the solution
   ```bash
   dotnet build
   ```

3. Run the API
   ```bash
   cd src/CryptoGPT.API
   dotnet run
   ```

4. Access the API at `https://localhost:7192/swagger`

## API Usage

### API Versioning

The API supports versioning through:
- Query string: `?api-version=1.0`
- Header: `X-Api-Version: 1.0`

### Endpoints

- **Coins**: `/api/coin` - Get cryptocurrency data
- **News**: `/api/news` - Get cryptocurrency news
- **Recommendations**: `/api/recommendation` - Get cryptocurrency recommendations
- **Health**: `/api/health` - API health checks

## Testing

Run the tests with:

```bash
dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.