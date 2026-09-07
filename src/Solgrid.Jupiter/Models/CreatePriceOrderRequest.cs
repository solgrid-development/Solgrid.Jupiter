namespace Solgrid.Jupiter.Models;

public sealed class CreatePriceOrderRequest
{
    public required TriggerOrderType OrderType { get; init; }

    public required string DepositRequestId { get; init; }

    public required string DepositSignedTx { get; init; }

    public required string UserPubkey { get; init; }

    public required string InputMint { get; init; }

    public required string InputAmount { get; init; }

    public required string OutputMint { get; init; }

    public required string TriggerMint { get; init; }

    // single/otoco: required. above needs triggerMint == outputMint, below needs triggerMint == inputMint
    public TriggerCondition? TriggerCondition { get; set; }

    // single: exactly one of TriggerPriceUsd / TrailingBps. otoco: required
    public double? TriggerPriceUsd { get; set; }

    // trailing stop loss, 50-9000 bps, single orders only
    public int? TrailingBps { get; set; }

    public int? SlippageBps { get; set; }

    // oco/otoco: take profit, must be greater than SlPriceUsd
    public double? TpPriceUsd { get; set; }

    public double? SlPriceUsd { get; set; }

    public int? TpSlippageBps { get; set; }

    public int? SlSlippageBps { get; set; }

    // epoch milliseconds, must be in the future; v2 orders always expire
    public required long ExpiresAt { get; init; }
}
