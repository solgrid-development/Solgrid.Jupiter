using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class FetcherReport
{
    public string Id { get; set; } = string.Empty;

    public string? Status { get; set; }

    public double? Duration { get; set; }

    public string? Error { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
