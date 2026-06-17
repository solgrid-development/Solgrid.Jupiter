using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TokenPrice
{
    public string? CreatedAt { get; set; }

    public double? Liquidity { get; set; }

    public double? UsdPrice { get; set; }

    public long? BlockId { get; set; }

    public int? Decimals { get; set; }

    public double? PriceChange24h { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
