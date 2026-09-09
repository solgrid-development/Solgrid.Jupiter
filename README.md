# Solgrid.Jupiter

Simple .NET 8 client for the [Jupiter](https://jup.ag) DeFi API on Solana:
swaps (V2 + Ultra), limit orders and DCA (Trigger V2), token prices,
metadata and portfolio positions. Plain HTTP, no Solana SDK dependency -
signing stays on your side.

## Why this library exists

Jupiter is the main DEX aggregator on Solana, but its official SDKs target
TypeScript/Python. I found no maintained C# client for the current API
surface ([Solnet.JupiterSwap](https://github.com/Bifrost-Technologies/Solnet.JupiterSwap)
retired), so I wrote this from the official OpenAPI specs and checked it
against live API responses.

## Demo app

A GUI demo for this library lives in
[@Zelwel0](https://github.com/Zelwel0)'s fork. Self-contained Windows build,
no .NET needed: [release v0.2.0](https://github.com/Zelwel0/Solgrid.Jupiter/releases/tag/v0.2.0)

<p align="center">
  <img src="https://raw.githubusercontent.com/Zelwel0/Solgrid.Jupiter/demo/docs/demo-ultra-quote.png" width="49%" alt="Ultra quote with fee breakdown"/>
  <img src="https://raw.githubusercontent.com/Zelwel0/Solgrid.Jupiter/demo/docs/demo-tokens.png" width="49%" alt="top organic tokens table"/>
</p>

## Coverage

Two clients, one per API base:

| Domain | Client | What it does |
|---|---|---|
| Swap V2 | `JupiterSwapClient` | quote + unsigned v0 tx (`/order`), raw instructions (`/build`), landing (`/execute`) |
| Ultra V1 | `JupiterSwapClient` | opinionated swap in one call, fee breakdown (signature/priority/rent), gasless support |
| Price V3 | `JupiterSwapClient` | USD prices, 24h change, liquidity - up to 50 mints per call |
| Tokens V2 | `JupiterSwapClient` | search, tags, top lists, new tokens - metadata + safety metrics |
| Portfolio V1 (beta) | `JupiterSwapClient` | Jupiter product positions, platforms, staked JUP |
| Trigger V2 | `JupiterTriggerClient` | limit orders (single/OCO/OTOCO, trailing) and DCA: challenge-response auth, vault, deposits, create/update/cancel/history |

## Quick start

You need the .NET 8 SDK, a Solana wallet (the secret key only when you sign
real swaps or orders) and a Jupiter API key from the
[Jupiter Developer Portal](https://developers.jup.ag/portal). The key is
free; Tokens V2 and Portfolio do not work without one.

```bash
git clone https://github.com/Lak1Lay/Solgrid.Jupiter
cd Solgrid.Jupiter/src && dotnet build -c Release
```

```csharp
using Solgrid.Jupiter;

using var client = new JupiterSwapClient(new JupiterSwapClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("JUPITER_API_KEY") // optional
});

var prices = await client.GetPricesAsync(new[]
{
    "So11111111111111111111111111111111111111112" // SOL
});
```

Keyless calls run at 0.5 requests per second, a free key raises that to
1 RPS. Both clients throttle to `MinRequestInterval` and retry once on
HTTP 429. Trigger V2 adds a challenge-response JWT on top of the key,
valid 24h with no refresh endpoint.

Full snippet for every endpoint, including signing and the Trigger V2 auth
flow: [EXAMPLES.md](EXAMPLES.md).

## Status

Pre-release. The public API may change until `1.0.0`.

## License

MIT
