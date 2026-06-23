using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class PortfolioTokenInfo
{
    public string? Address { get; set; }

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    public int? Decimals { get; set; }

    public string? LogoUri { get; set; }

    public List<string>? Tags { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
