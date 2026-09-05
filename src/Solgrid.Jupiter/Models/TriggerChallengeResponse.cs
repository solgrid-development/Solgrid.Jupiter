using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerChallengeResponse
{
    public string? Type { get; set; }

    public string? Challenge { get; set; }

    public string? Transaction { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
