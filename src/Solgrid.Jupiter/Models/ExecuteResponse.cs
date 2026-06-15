using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class ExecuteResponse
{
    public string? Status { get; set; }

    public string? Signature { get; set; }

    public string? Slot { get; set; }

    public string? Error { get; set; }

    public int? Code { get; set; }

    public string? TotalInputAmount { get; set; }

    public string? TotalOutputAmount { get; set; }

    public string? InputAmountResult { get; set; }

    public string? OutputAmountResult { get; set; }

    public List<SwapEvent>? SwapEvents { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonIgnore]
    public bool IsSuccess => string.Equals(Status, "Success", StringComparison.Ordinal);
}

public sealed class SwapEvent
{
    public string? InputMint { get; set; }

    public string? InputAmount { get; set; }

    public string? OutputMint { get; set; }

    public string? OutputAmount { get; set; }
}
