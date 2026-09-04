using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class UltraRouteSwapInfo
{
    public string? AmmKey { get; set; }

    public string? Label { get; set; }

    public string? InputMint { get; set; }

    public string? OutputMint { get; set; }

    public string? InAmount { get; set; }

    public string? OutAmount { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class UltraRouteStep
{
    public int? Percent { get; set; }

    public int? Bps { get; set; }

    public double? UsdValue { get; set; }

    public UltraRouteSwapInfo? SwapInfo { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
