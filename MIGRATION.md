# Migration to Clean Architecture with Minimal APIs

This document outlines the migration of CryptoGPT.Net from a traditional controller-based architecture to Clean Architecture using Minimal APIs.

## Clean Architecture Structure

The solution has been restructured into the following layers:

- **CryptoGPT.Domain**: Contains core entities and business rules
- **CryptoGPT.Application**: Contains application logic, use cases and interfaces
- **CryptoGPT.Infrastructure**: Contains implementations of interfaces defined in the Application layer
- **CryptoGPT.API**: Contains Minimal API endpoints and API configuration

Each layer has a corresponding test project in the `tests` directory.

## Key Changes

### 1. Domain Layer

The Domain layer contains the core business entities without any external dependencies:
- Core entity models like `CryptoCurrency`, `CryptoCurrencyDetail`, etc.
- Domain-specific exceptions
- Value objects and domain events (if applicable)

### 2. Application Layer

The Application layer contains:
- Interfaces for all services (moved from Core project)
- DTOs for data transfer across boundaries
- MediatR handlers implementing CQRS pattern:
  - Queries (e.g., `GetTopCoinsQuery`, `GetCoinDetailsQuery`)
  - Commands (to be implemented as needed)
- Validators using FluentValidation

### 3. Infrastructure Layer

The Infrastructure layer implements interfaces defined in the Application layer:
- External API clients (CoinGecko, CoinCap, etc.)
- Caching implementations
- Technical services like logging, serialization

### 4. API Layer

The API layer has been converted from traditional controllers to Minimal APIs:
- Endpoint definitions grouped by feature
- Simpler routing and handler implementation
- Reduced boilerplate code
- Better performance
- API versioning with support for multiple versions

## Minimal APIs vs. Controllers

### Before (Controller-based):

```csharp
[ApiController]
[Route("api/[controller]")]
public class CoinController : ControllerBase
{
    private readonly ICryptoDataService _cryptoDataService;
    
    public CoinController(ICryptoDataService cryptoDataService)
    {
        _cryptoDataService = cryptoDataService;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetTopCoins([FromQuery] int? limit)
    {
        var coins = await _cryptoDataService.GetTopCoinsAsync(limit ?? 10);
        return Ok(coins);
    }
}
```

### After (Minimal APIs):

```csharp
app.MapGet("/api/coin", async (IMediator mediator, int? limit) =>
{
    var result = await mediator.Send(new GetTopCoinsQuery { Limit = limit ?? 10 });
    return Results.Ok(result);
})
.WithName("GetTopCoins")
.WithDescription("Get top cryptocurrencies by market cap");
```

## Features Added During Migration

### 1. CQRS with MediatR

Command Query Responsibility Segregation pattern has been implemented using MediatR:
- Queries for reading data (e.g., `GetTopCoinsQuery`)
- Commands for modifying data (to be implemented as needed)

### 2. Validation with FluentValidation

Input validation using FluentValidation:
- Validators for all queries and commands
- Validation pipeline behavior for MediatR
- Consistent validation error responses

### 3. API Versioning

API versioning has been implemented:
- Multiple version support (1.0, 2.0, etc.)
- Version specification via query string (`api-version=1.0`)
- Version specification via HTTP header (`X-Api-Version: 1.0`)
- Default version when unspecified

### 4. Error Handling

Consistent error handling across the API:
- Custom middleware for error handling
- Different responses for different exception types
- Validation errors return proper validation problem details
- Structured error responses

## Testing

### Unit Testing

Unit tests are implemented for each layer using xUnit:
- Domain entities are tested in `CryptoGPT.Domain.Tests`
- Application use cases are tested in `CryptoGPT.Application.Tests`
- Infrastructure implementations are tested in `CryptoGPT.Infrastructure.Tests`

Example unit test for a query handler:

```csharp
[Fact]
public async Task Handle_ShouldReturnCorrectNumberOfCoins()
{
    // Arrange
    int limit = 5;
    var query = new GetTopCoinsQuery { Limit = limit };
    
    _mockCryptoDataService
        .Setup(service => service.GetTopCoinsAsync(limit))
        .ReturnsAsync(mockCoins);

    // Act
    var result = await _sut.Handle(query, CancellationToken.None);

    // Assert
    Assert.Equal(limit, result.Count);
}
```

### Integration Testing

Integration tests use `WebApplicationFactory` to test the entire API pipeline:

```csharp
[Fact]
public async Task GetTopCoins_ReturnsExpectedCoins()
{
    // Act
    var coins = await _client.GetFromJsonAsync<List<CryptoCurrencyDto>>("/api/coin");

    // Assert
    Assert.NotNull(coins);
    Assert.Equal(2, coins.Count);
}
```

## Benefits of the Migration

1. **Better Separation of Concerns**: Each layer has clear responsibilities
2. **Improved Testability**: Easier to unit test individual components
3. **Domain-Centric Design**: Business rules are isolated from infrastructure concerns
4. **More Maintainable**: Changes in one layer don't affect others
5. **Reduced Boilerplate**: Minimal APIs reduce code verbosity
6. **Better Performance**: Minimal APIs are more performant than controllers
7. **Versioning Support**: API versioning ensures backward compatibility
8. **Validation**: Consistent validation across all endpoints
9. **Error Handling**: Standardized error responses throughout the application

## Next Steps

1. Complete the implementation of all use cases
2. Refine error handling and validation
3. Add authentication/authorization
4. Implement background processing for long-running tasks