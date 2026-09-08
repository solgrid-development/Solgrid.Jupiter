namespace Solgrid.Jupiter.Models;

public sealed class CreateDcaOrderRequest
{
    public required string DepositRequestId { get; init; }

    public required string DepositSignedTx { get; init; }

    public required string UserPubkey { get; init; }

    public required string InputMint { get; init; }

    public required string OutputMint { get; init; }

    // total budget in smallest units; dust from uneven division goes to the last round
    public required string InputAmount { get; init; }

    // minimum 2 rounds, each worth at least 10 USD
    public required int OrderCount { get; init; }

    // 60 to 31536000 (1 minute to 1 year)
    public required int IntervalSeconds { get; init; }

    // null lets the api default to time_based
    public DcaOrderType? OrderType { get; set; }

    // price_conditional only, at least one bound plus TriggerMint
    public double? MinPriceUsd { get; set; }

    public double? MaxPriceUsd { get; set; }

    public string? TriggerMint { get; set; }

    // ISO-8601, defaults to now, at most 30 days out
    public string? BeginFillAt { get; set; }

    // Earn While You Wait: time_based + supported stablecoin input only,
    // deposit must be crafted with the same JlMint
    public bool? JlEnabled { get; set; }

    public string? JlMint { get; set; }
}
