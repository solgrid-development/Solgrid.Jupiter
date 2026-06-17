using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TokenSwapStats
{
    public double? PriceChange { get; set; }

    public double? HolderChange { get; set; }

    public double? LiquidityChange { get; set; }

    public double? VolumeChange { get; set; }

    public double? BuyVolume { get; set; }

    public double? SellVolume { get; set; }

    public double? BuyOrganicVolume { get; set; }

    public double? SellOrganicVolume { get; set; }

    public long? NumBuys { get; set; }

    public long? NumSells { get; set; }

    public long? NumTraders { get; set; }

    public long? NumOrganicBuyers { get; set; }

    public long? NumNetBuyers { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
