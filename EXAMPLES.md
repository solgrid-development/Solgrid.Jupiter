# Examples

One snippet per API domain. Swap-side calls use `JupiterSwapClient`,
limit orders and DCA use `JupiterTriggerClient`:

```csharp
using Solgrid.Jupiter;
using Solgrid.Jupiter.Models;

using var client = new JupiterSwapClient(new JupiterSwapClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("JUPITER_API_KEY"), // optional
    MinRequestInterval = TimeSpan.FromMilliseconds(1100)
});

var triggerOptions = new JupiterTriggerClientOptions
{
    ApiKey = Environment.GetEnvironmentVariable("JUPITER_API_KEY")
};
using var trigger = new JupiterTriggerClient(triggerOptions);
```

All amounts are raw token units (integers, scaled by the mint decimals):
1 SOL = `1000000000`, 1 USDC = `1000000`.

## Prices (Price V3)

Up to 50 mints per call, works without an API key:

```csharp
var prices = await client.GetPricesAsync(new[]
{
    "So11111111111111111111111111111111111111112", // SOL
    "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"  // USDC
});

foreach (var (mint, price) in prices)
    Console.WriteLine($"{mint}: ${price.UsdPrice} (24h {price.PriceChange24h:F2}%)");
```

## Swap V2 (quote, sign, execute)

`/order` returns a quote plus an assembled unsigned v0 transaction when you
pass a `taker`. Signing below uses [Solnet](https://github.com/bmresearch/Solnet)
(`Solana.Rpc` NuGet package, 8.7.0+):

```csharp
using Solnet.Rpc.Models;
using Solnet.Wallet;

var account = Account.FromSecretKey(secretKey); // your wallet

var order = await client.GetOrderAsync(new OrderRequest
{
    InputMint  = "So11111111111111111111111111111111111111112",
    OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    Amount     = "1000000000",              // 1 SOL
    Taker      = account.PublicKey.Key      // omit for quote-only
});

if (!order.HasTransaction)
{
    Console.WriteLine($"quote only: {order.OutAmount} out, router {order.Router}");
    return;
}

// partial-sign the v0 transaction (some routes are co-signed by a market maker)
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

Console.WriteLine(result.IsSuccess ? $"landed: {result.Signature}" : $"failed: {result.Error}");
```

`/build` returns raw instructions plus address lookup tables for callers
that assemble and send transactions themselves (own RPC, CPI, arbitrage):

```csharp
var build = await client.GetBuildAsync(new BuildRequest
{
    InputMint = solMint,
    OutputMint = usdcMint,
    Amount = "1000000000",
    Taker = account.PublicKey.Key
});

Console.WriteLine($"{build.SetupInstructions?.Count} setup, swap present: {build.SwapInstruction is not null}, " +
                  $"{build.AddressesByLookupTableAddress?.Count} lookup tables");
```

## Ultra V1

One call for quote + transaction, with the fee breakdown (signature,
prioritization, rent) exposed. Same sign-then-execute flow:

```csharp
var ultra = await client.GetUltraOrderAsync(new UltraOrderRequest
{
    InputMint  = "So11111111111111111111111111111111111111112",
    OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    Amount     = "1000000000",
    Taker      = account.PublicKey.Key
});

Console.WriteLine($"router {ultra.Router}, out {ultra.OutAmount}, " +
                  $"rent fee {ultra.RentFeeLamports} lamports, gasless {ultra.Gasless}");

// ... sign exactly like the Swap V2 example, then:
var executed = await client.UltraExecuteAsync(new UltraExecuteRequest
{
    SignedTransaction = signedBase64,
    RequestId = ultra.RequestId
});
```

## Trigger V2: limit orders and DCA

Limit orders and DCA live under `/trigger/v2`. Auth is challenge-response:
the wallet signs a message, the API returns a JWT valid for 24h (no refresh
endpoint, re-run the flow when it expires). Deposits go into a
Privy-managed vault shared by all your orders.

```csharp
using System.Text;
using Solnet.Wallet.Utilities;

var challenge = await trigger.GetChallengeAsync(account.PublicKey.Key);
var signature = account.Sign(Encoding.UTF8.GetBytes(challenge.Challenge!));
var verify = await trigger.VerifyAsync(new TriggerVerifyRequest
{
    Type = TriggerChallengeType.Message,
    WalletPubkey = account.PublicKey.Key,
    Signature = Encoders.Base58.EncodeData(signature)
});
triggerOptions.AuthToken = verify.Token;

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
milliseconds and a deposit worth at least 10 USD.

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

DCA splits one deposit into rounds that the Jupiter keeper swaps on a
schedule. Same vault and auth, deposit is crafted with
`TriggerDepositOrderType.Dca` and no subtype:

```csharp
var dcaDeposit = await trigger.CraftDepositAsync(new CraftDepositRequest
{
    InputMint   = "So11111111111111111111111111111111111111112",
    OutputMint  = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    UserAddress = account.PublicKey.Key,
    Amount      = "200000000",   // 0.2 SOL; every round needs at least 10 USD
    OrderType   = TriggerDepositOrderType.Dca
});
// sign dcaDeposit.Transaction the same way as above

var dca = await trigger.CreateDcaOrderAsync(new CreateDcaOrderRequest
{
    DepositRequestId = dcaDeposit.RequestId!,
    DepositSignedTx  = signedDcaDepositTx,
    UserPubkey       = account.PublicKey.Key,
    InputMint        = "So11111111111111111111111111111111111111112",
    OutputMint       = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
    InputAmount      = "200000000",
    OrderCount       = 2,        // rounds, minimum 2
    IntervalSeconds  = 3600,     // 60 seconds to 1 year
    OrderType        = DcaOrderType.TimeBased
});

var active = await trigger.GetDcaHistoryAsync(new DcaHistoryQuery
{
    State = TriggerHistoryState.Active
});
var one = await trigger.GetDcaOrderAsync(dca.Id!);   // roundsFilled, fillPercent, events
```

`PriceConditional` orders fill a round only while the USD price of
`TriggerMint` stays inside `MinPriceUsd`/`MaxPriceUsd`. `BeginFillAt`
(ISO-8601, up to 30 days out) delays the first round. `JlEnabled` with
`JlMint` parks the idle stablecoin in Jupiter Lend between rounds
(time-based orders with a supported stablecoin input only).

DCA orders cannot be edited, only cancelled. Same two-step flow as limit
orders, and the response tells you what comes back:

```csharp
var cancel = await trigger.CancelDcaOrderAsync(dca.Id!);
// cancel.RefundAmount is the unfilled remainder, cancel.RoundsRemaining the skipped rounds
var confirmed = await trigger.ConfirmCancelDcaOrderAsync(dca.Id!, new ConfirmCancelRequest
{
    SignedTransaction = signedWithdrawalTx,   // cancel.Transaction, signed
    CancelRequestId = cancel.RequestId!
});
```

## Tokens (Tokens V2, needs API key)

```csharp
var found = await client.SearchTokensAsync("SOL");
foreach (var token in found)
    Console.WriteLine($"{token.Symbol}: verified={token.IsVerified}, " +
                      $"holders={token.HolderCount}, organic={token.OrganicScore:F1}");

var top = await client.GetTopTokensAsync(TokenCategory.TopOrganicScore, TokenInterval.TwentyFourHours, 25);
var tagged = await client.GetTokensByTagAsync(TokenTag.Lst);
var fresh = await client.GetRecentTokensAsync();
```

## Portfolio (Portfolio V1 beta, needs API key)

Positions are Jupiter product positions (limit orders, DCA, perps,
launchpad, staked JUP), not raw wallet balances:

```csharp
var portfolio = await client.GetPortfolioAsync(account.PublicKey.Key);
foreach (var element in portfolio.Elements ?? new())
    Console.WriteLine($"{element.Label} @ {element.PlatformId}: ${element.Value:F2}");

var platforms = await client.GetPlatformsAsync();
var staked = await client.GetStakedJupAsync(account.PublicKey.Key);
Console.WriteLine($"staked JUP: {staked.StakedAmount}");
```

For raw SOL/SPL balances use any Solana RPC (e.g. Solnet
`IRpcClient.GetBalanceAsync` / `GetTokenAccountsByOwnerAsync`).

## Errors and rate limits

HTTP failures throw `JupiterApiException` with `StatusCode`, `Error`,
`RequestId` and the Jupiter `Code` when present:

```csharp
try
{
    await client.GetPricesAsync(mints);
}
catch (JupiterApiException ex)
{
    Console.WriteLine($"HTTP {ex.StatusCode}: {ex.Error} (request {ex.RequestId}, code {ex.Code})");
}
```

Keyless access runs at 0.5 RPS, a free key from the
[Jupiter Developer Portal](https://developers.jup.ag/portal) raises it to
1 RPS. The clients space requests by `MinRequestInterval` and retry once
on HTTP 429 using `x-ratelimit-reset`. Tokens V2 and Portfolio require a
key; Trigger V2 works keyless at reduced limits but the JWT flow is the
same either way.
