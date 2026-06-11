using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class PlatformFee
{
    public string? Amount { get; set; }

    public int? FeeBps { get; set; }

    public string? FeeMint { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
