using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class OrderResponse
{
    public string? Mode { get; set; }

    public string? InputMint { get; set; }

    public string? OutputMint { get; set; }

    public string? InAmount { get; set; }

    public string? OutAmount { get; set; }

    public double? InUsdValue { get; set; }

    public double? OutUsdValue { get; set; }

    public double? PriceImpact { get; set; }

    public double? SwapUsdValue { get; set; }

    public string? OtherAmountThreshold { get; set; }

    public string? SwapMode { get; set; }

    public int? SlippageBps { get; set; }

    public string? PriceImpactPct { get; set; }

    public List<RoutePlanStep>? RoutePlan { get; set; }

    public string? ReferralAccount { get; set; }

    public string? FeeMint { get; set; }

    public int? FeeBps { get; set; }

    public PlatformFee? PlatformFee { get; set; }

    public long? SignatureFeeLamports { get; set; }

    public string? SignatureFeePayer { get; set; }

    public long? PrioritizationFeeLamports { get; set; }

    public string? PrioritizationFeePayer { get; set; }

    public long? RentFeeLamports { get; set; }

    public string? RentFeePayer { get; set; }

    public string? SwapType { get; set; }

    public string? Router { get; set; }

    public string? Transaction { get; set; }

    public string? LastValidBlockHeight { get; set; }

    public bool? Gasless { get; set; }

    public string? RequestId { get; set; }

    public double? TotalTime { get; set; }

    public string? Taker { get; set; }

    public string? QuoteId { get; set; }

    public string? Maker { get; set; }

    public string? ExpireAt { get; set; }

    public int? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Error { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonIgnore]
    public bool HasTransaction => !string.IsNullOrEmpty(Transaction);
}
