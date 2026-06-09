# Solgrid.Jupiter

Simple .NET 8 client library for the [Jupiter](https://jup.ag) DeFi API on Solana —
swap quotes, token prices, token metadata and portfolio positions in one HTTP client.

## Why this library exists

Jupiter is the main DEX aggregator on Solana, but its official SDKs target
TypeScript/Python. There is no maintained, up-to-date C# client covering the
current API surface ([Solnet.JupiterSwap](https://github.com/Bifrost-Technologies/Solnet.JupiterSwap) retired), so this library was written from the official OpenAPI
specs and verified against live API responses.

## Current API coverage

| Domain | Endpoints | Client methods |
|---|---|---|
| Swap V2 | `GET /order`, `GET /build`, `POST /execute` | `GetOrderAsync`, `GetBuildAsync`, `ExecuteAsync` |
| Price V3 | `GET /price/v3` | `GetPricesAsync` |
| Tokens V2 | `GET /search`, `GET /tag`, `GET /{category}/{interval}`, `GET /recent` | `SearchTokensAsync`, `GetTokensByTagAsync`, `GetTopTokensAsync`, `GetRecentTokensAsync` |
| Portfolio V1 (beta) | `GET /positions/{address}`, `GET /platforms`, `GET /staked-jup/{address}` | `GetPortfolioAsync`, `GetPlatformsAsync`, `GetStakedJupAsync` |

## Usage

### Token prices

```csharp
var prices = await client.GetPricesAsync(new[]
{
    "So11111111111111111111111111111111111111112", // SOL
    "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"  // USDC
});

foreach (var (mint, price) in prices)
    Console.WriteLine($"{mint}: ${price.UsdPrice}");
```

### Swap quote and execution

`/order` returns a quote plus an assembled, **unsigned** v0 transaction.
Sign it with your wallet stack, then let Jupiter land it via `/execute`:

```csharp
using Solgrid.Jupiter.Models;

var order = await client.GetOrderAsync(new OrderRequest
{
    InputMint  = "So11111111111111111111111111111111111111112",
    OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    Amount     = "1000000",                       // raw amount (1 SOL)
    Taker      = "YOUR_WALLET_ADDRESS"            // omit for quote-only
});

if (order.HasTransaction)
{
    // string signedTx = <sign order.Transaction with your wallet>;
    var result = await client.ExecuteAsync(new ExecuteRequest
    {
        SignedTransaction = signedTx,
        RequestId = order.RequestId
    });
    Console.WriteLine(result.IsSuccess ? $"swap landed: {result.Signature}" : result.Error);
}
```

### Token metadata and safety checks

```csharp
var tokens = await client.SearchTokensAsync("SOL");
foreach (var token in tokens)
    Console.WriteLine($"{token.Symbol}: verified={token.IsVerified}, " +
                      $"organic score={token.OrganicScore}, holders={token.HolderCount}");

var top   = await client.GetTopTokensAsync(TokenCategory.TopOrganicScore, TokenInterval.TwentyFourHours, 25);
var fresh = await client.GetRecentTokensAsync();
```

### Portfolio

```csharp
var portfolio = await client.GetPortfolioAsync("WALLET_ADDRESS");
foreach (var element in portfolio.Elements ?? new())
    Console.WriteLine($"{element.Label} @ {element.PlatformId}: ${element.Value}");

var staked = await client.GetStakedJupAsync("WALLET_ADDRESS");
Console.WriteLine($"staked JUP: {staked.StakedAmount}");
```

> Portfolio API returns **Jupiter product positions** (open limit
> orders, DCA, staked JUP, perps, launchpad), **NOT** raw wallet balances.

## Rate limits

Keyless access works at 0.5 RPS; a free API key from the
[Jupiter Developer Portal](https://developers.jup.ag/portal) raises it to
1 RPS (higher tiers available). The client throttles requests to
`MinRequestInterval` and retries once on HTTP 429. Tokens V2 and Portfolio
require an API key.

## Status

`0.1.0` pre-release. The public API may change until `1.0.0`. Advanced
`/build` transaction assembly (composing custom instructions around swap
routes) is intentionally out of scope for now.

## License

MIT
