using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class PortfolioElement
{
    public string Type { get; set; } = string.Empty;

    public string? NetworkId { get; set; }

    public string? PlatformId { get; set; }

    public double? Value { get; set; }

    public string? Label { get; set; }

    public string? Name { get; set; }

    public List<string>? Tags { get; set; }

    public double? NetApy { get; set; }

    // data is one of ~5 shapes depending on Type; kept as raw JSON until
    // the typed variants are actually needed
    public JsonElement? Data { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
