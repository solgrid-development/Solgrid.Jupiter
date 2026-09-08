using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class DcaHistoryItem
{
    public string? Id { get; set; }

    public string? RequestId { get; set; }

    public string? UserPubkey { get; set; }

    public string? VaultPubkey { get; set; }

    public string? InputMint { get; set; }

    public string? OutputMint { get; set; }

    public string? InputAmountInitial { get; set; }

    public string? InputAmountRemaining { get; set; }

    public string? AmountPerRound { get; set; }

    public string? OutputAmountTotal { get; set; }

    public string? InputAmountUsed { get; set; }

    // time_based, price_conditional
    public string? OrderType { get; set; }

    public double? MinPriceUsd { get; set; }

    public double? MaxPriceUsd { get; set; }

    public string? TriggerMint { get; set; }

    public int? RetryWindowSeconds { get; set; }

    public bool? JlEnabled { get; set; }

    public double? JlYieldUsd { get; set; }

    public int? NumberOfRounds { get; set; }

    public int? IntervalSeconds { get; set; }

    // ISO-8601 timestamps
    public string? BeginFillAt { get; set; }

    public string? NextFillAt { get; set; }

    public string? LastFillAt { get; set; }

    public int? RoundsFilled { get; set; }

    // 0 to 1
    public double? FillPercent { get; set; }

    // depositing, deposit_failed, active, executing, withdrawing, completed, cancelled
    public string? State { get; set; }

    // pending, active, executing, pending_withdraw, completed, cancelled, failed
    public string? DisplayState { get; set; }

    public string? CreatedAt { get; set; }

    public string? UpdatedAt { get; set; }

    public List<DcaOrderEvent>? Events { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
