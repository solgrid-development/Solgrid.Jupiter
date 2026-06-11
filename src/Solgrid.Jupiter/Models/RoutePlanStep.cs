using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class RoutePlanStep
{
    public SwapInfo? SwapInfo { get; set; }

    public double? Percent { get; set; }

    public double? Bps { get; set; }

    public double? UsdValue { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
