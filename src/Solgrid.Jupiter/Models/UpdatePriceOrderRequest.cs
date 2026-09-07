namespace Solgrid.Jupiter.Models;

public sealed class UpdatePriceOrderRequest
{
    public required TriggerOrderType OrderType { get; init; }

    // cannot be set on a trailing order
    public double? TriggerPriceUsd { get; set; }

    // only for an order that is already trailing
    public int? TrailingBps { get; set; }

    public int? SlippageBps { get; set; }

    public double? TpPriceUsd { get; set; }

    public double? SlPriceUsd { get; set; }

    public int? TpSlippageBps { get; set; }

    public int? SlSlippageBps { get; set; }
}
