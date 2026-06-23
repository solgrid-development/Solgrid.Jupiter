using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class PortfolioResponse
{
    public long? Date { get; set; }

    public string? Owner { get; set; }

    public List<FetcherReport>? FetcherReports { get; set; }

    public List<PortfolioElement>? Elements { get; set; }

    public double? Duration { get; set; }

    public Dictionary<string, Dictionary<string, PortfolioTokenInfo>>? TokenInfo { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
