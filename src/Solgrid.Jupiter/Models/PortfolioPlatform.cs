using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class PortfolioPlatform
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Image { get; set; }

    public string? Description { get; set; }

    public string? DefiLlamaId { get; set; }

    public bool? IsDeprecated { get; set; }

    public List<string>? Tokens { get; set; }

    public List<string>? Tags { get; set; }

    public PlatformLinks? Links { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class PlatformLinks
{
    public string? Website { get; set; }

    public string? Discord { get; set; }

    public string? Telegram { get; set; }

    public string? Twitter { get; set; }

    public string? Github { get; set; }

    public string? Medium { get; set; }

    public string? Documentation { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
