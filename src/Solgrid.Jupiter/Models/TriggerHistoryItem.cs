using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerHistoryItem
{
    public string? Id { get; set; }

    public string? OrderType { get; set; }

    // display state: pending, open, executing, filled, pending_withdraw, cancelled, expired, failed
    public string? OrderState { get; set; }

    public string? RawState { get; set; }

    public string? UserPubkey { get; set; }

    public string? PrivyWalletPubkey { get; set; }

    public string? InputMint { get; set; }

    public string? InitialInputAmount { get; set; }

    public string? RemainingInputAmount { get; set; }

    public string? OutputMint { get; set; }

    public string? TriggerMint { get; set; }

    public string? TriggerCondition { get; set; }

    public double? TriggerPriceUsd { get; set; }

    public int? SlippageBps { get; set; }

    public long? ExpiresAt { get; set; }

    public long? CreatedAt { get; set; }

    public long? UpdatedAt { get; set; }

    public long? TriggeredAt { get; set; }

    public string? OutputAmount { get; set; }

    public string? InputUsed { get; set; }

    public double? FillPercent { get; set; }

    // trailing stop loss orders only
    public int? TrailingBps { get; set; }

    public double? HighWatermark { get; set; }

    public double? LowWatermark { get; set; }

    public List<TriggerOrderEvent>? Events { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
