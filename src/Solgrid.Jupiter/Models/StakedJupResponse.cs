using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class StakedJupResponse
{
    public double? StakedAmount { get; set; }

    public List<JupUnstakingEntry>? Unstaking { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class JupUnstakingEntry
{
    public double? Amount { get; set; }

    public long? Until { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
