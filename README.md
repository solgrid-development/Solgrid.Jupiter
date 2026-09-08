# Solgrid.Jupiter

Simple .NET 8 client library for the [Jupiter](https://jup.ag) DeFi API on Solana to swap quotes, token prices, token metadata and portfolio positions in one HTTP client.

## Why this library exists

Jupiter is the main DEX aggregator on Solana, but its official SDKs target TypeScript/Python. There is no maintained, up-to-date C# client covering the current API surface, so this library was written from the official OpenAPI specs and verified against live API responses.

## Current API coverage

| Domain | Endpoints | Client methods |
|---|---|---|
| Swap V2 | `GET /order`, `GET /build`, `POST /execute` | `GetOrderAsync`, `GetBuildAsync`, `ExecuteAsync` |
| Price V3 | `GET /price/v3` | `GetPricesAsync` |
| Tokens V2 | `GET /search`, `GET /tag`, `GET /{category}/{interval}`, `GET /recent` | `SearchTokensAsync`, `GetTokensByTagAsync`, `GetTopTokensAsync`, `GetRecentTokensAsync` |
| Portfolio V1 (beta) | `GET /positions/{address}`, `GET /platforms`, `GET /staked-jup/{address}` | `GetPortfolioAsync`, `GetPlatformsAsync`, `GetStakedJupAsync` |
| Trigger V2 | `POST /auth/challenge`, `POST /auth/verify`, `GET /vault`, `GET /vault/register`, `POST /deposit/craft`, `POST /orders/price`, `PATCH /orders/price/{id}`, `POST /orders/price/cancel/{id}`, `POST /orders/price/confirm-cancel/{id}`, `GET /orders/history` | `JupiterTriggerClient`: `GetChallengeAsync`, `VerifyAsync`, `GetVaultAsync`, `RegisterVaultAsync`, `CraftDepositAsync`, `CreatePriceOrderAsync`, `UpdatePriceOrderAsync`, `CancelPriceOrderAsync`, `ConfirmCancelPriceOrderAsync`, `GetOrderHistoryAsync` |

## Getting started

### Adding it to your project

The package is not on NuGet yet — reference the project directly:

```bash
git clone https://github.com/Lak1Lay/Solgrid.Jupiter
dotnet add YourApp.csproj reference Solgrid.Jupiter/src/Solgrid.Jupiter/Solgrid.Jupiter.csproj
```

The library itself has one dependency: `Microsoft.Extensions.Logging.Abstractions`.

### Creating a wallet account

The library is pure HTTP and never touches keys. Signing is done by any
Solana SDK with v0 (versioned transaction) support; the examples here use
[Solnet](https://github.com/bmresearch/Solnet), published as the `Solana.*`
NuGet packages:

```bash
dotnet add package Solana.Rpc --version 8.7.0
```

```csharp
using Solnet.Wallet;
using Solnet.Wallet.Bip39;

// brand-new account from a random 24-word mnemonic
var mnemonic = new Mnemonic(WordList.English, WordCount.TwentyFour);
var account = new Wallet(mnemonic).Account;
Console.WriteLine($"address: {account.PublicKey.Key}");
// keep mnemonic.ToString() safe — it is the only backup of the key

// restore an existing account from its mnemonic
var restored = new Wallet(new Mnemonic("word1 word2 ... word24")).Account;

// or import a solana-keygen style secret key (JSON byte array or base58)
var imported = Account.FromSecretKey(secretKeyString);
```

### Create a client

API key is optional, but raises rate limits:

```csharp
using Solgrid.Jupiter;

using var client = new JupiterSwapClient(new JupiterSwapClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("JUPITER_API_KEY"),
    MinRequestInterval = TimeSpan.FromMilliseconds(1100)
});
```

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
using Solnet.Rpc.Models;

var order = await client.GetOrderAsync(new OrderRequest
{
    InputMint  = "So11111111111111111111111111111111111111112",
    OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    Amount     = "1000000",                       // raw amount (1 SOL)
    Taker      = account.PublicKey.Key            // omit for quote-only
});

if (order.HasTransaction)
{
    // partially sign the v0 transaction with our key
    var tx = VersionedTransaction.Deserialize(order.Transaction);
    var slot = tx.Signatures.FindIndex(s => s.PublicKey.Key == account.PublicKey.Key);
    var message = tx.CompileMessage();
    tx.Signatures[slot] = new SignaturePubKeyPair
    {
        PublicKey = account.PublicKey,
        Signature = account.Sign(message)
    };

    var result = await client.ExecuteAsync(new ExecuteRequest
    {
        SignedTransaction = Convert.ToBase64String(tx.Serialize()),
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

### Trigger V2: limit orders

Limit orders live under `/trigger/v2` and use a separate
`JupiterTriggerClient`. Auth is challenge-response: the wallet signs a
message, the API returns a JWT valid for 24h (no refresh endpoint, re-run
the flow when it expires). Deposits go into a Privy-managed vault shared by
all your orders.

```csharp
using System.Text;
using Solgrid.Jupiter;
using Solgrid.Jupiter.Models;
using Solnet.Wallet.Utilities;

var options = new JupiterTriggerClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("JUPITER_API_KEY")
};
using var trigger = new JupiterTriggerClient(options);

var challenge = await trigger.GetChallengeAsync(account.PublicKey.Key);
var signature = account.Sign(Encoding.UTF8.GetBytes(challenge.Challenge!));
var verify = await trigger.VerifyAsync(new TriggerVerifyRequest
{
    Type = TriggerChallengeType.Message,
    WalletPubkey = account.PublicKey.Key,
    Signature = Encoders.Base58.EncodeData(signature)
});
options.AuthToken = verify.Token;

// null when the wallet has no vault yet
var vault = await trigger.GetVaultAsync() ?? await trigger.RegisterVaultAsync();

var deposit = await trigger.CraftDepositAsync(new CraftDepositRequest
{
    InputMint    = "So11111111111111111111111111111111111111112",
    OutputMint   = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    UserAddress  = account.PublicKey.Key,
    Amount       = "110000000",   // 0.11 SOL; orders have a 10 USD minimum
    OrderType    = TriggerDepositOrderType.Price,
    OrderSubType = TriggerOrderType.Single
});
```

`deposit.Transaction` is an unsigned transaction in legacy format (unlike
the v0 transactions from the swap endpoints). Sign it with your wallet; the
signed tx plus `deposit.RequestId` feed the create call:

```csharp
var order = await trigger.CreatePriceOrderAsync(new CreatePriceOrderRequest
{
    OrderType        = TriggerOrderType.Single,
    DepositRequestId = deposit.RequestId!,
    DepositSignedTx  = signedDepositTx,   // base64 of the signed deposit.Transaction
    UserPubkey       = account.PublicKey.Key,
    InputMint        = "So11111111111111111111111111111111111111112",
    InputAmount      = "110000000",
    OutputMint       = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    TriggerMint      = "So11111111111111111111111111111111111111112",
    TriggerCondition = TriggerCondition.Above,
    TriggerPriceUsd  = 200,               // sell SOL above $200
    SlippageBps      = 100,
    ExpiresAt        = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds()
});
// the deposit lands on-chain during this call; DepositConfirmed = true
// means the order is live
```

`Oco` orders take `TpPriceUsd`/`SlPriceUsd` instead of a single trigger
price, `Otoco` takes a parent trigger plus the TP/SL pair. A `Single` order
becomes a trailing stop when you set `TrailingBps` (50-9000) instead of
`TriggerPriceUsd`. Every order needs a future `ExpiresAt` in epoch
milliseconds and a deposit worth at least 10 USD (validated at craft time
already).

Track, edit and cancel:

```csharp
var open = await trigger.GetOrderHistoryAsync(new TriggerHistoryQuery
{
    State = TriggerHistoryState.Active
});

await trigger.UpdatePriceOrderAsync(order.Id!, new UpdatePriceOrderRequest
{
    OrderType = TriggerOrderType.Single,
    TriggerPriceUsd = 210
});

// cancel is two-step: fetch the withdrawal tx, sign it, confirm
var cancel = await trigger.CancelPriceOrderAsync(order.Id!);
var confirmed = await trigger.ConfirmCancelPriceOrderAsync(order.Id!, new ConfirmCancelRequest
{
    SignedTransaction = signedWithdrawalTx,   // cancel.Transaction, signed
    CancelRequestId = cancel.RequestId!
});
// funds are back in the wallet once confirmed.TxSignature lands
```

DCA endpoints are work in progress, see issue #1.

## Rate limits

Keyless access works at 0.5 RPS; a free API key from the
[Jupiter Developer Portal](https://developers.jup.ag/portal) raises it to
1 RPS (higher tiers available). The client throttles requests to
`MinRequestInterval` and retries once on HTTP 429. Tokens V2 and Portfolio
require an API key; Trigger V2 works keyless at reduced limits but the JWT
flow is the same either way.

## Status

`0.1.0` pre-release. The public API may change until `1.0.0`. Advanced
`/build` transaction assembly (composing custom instructions around swap
routes) is intentionally out of scope for now.
