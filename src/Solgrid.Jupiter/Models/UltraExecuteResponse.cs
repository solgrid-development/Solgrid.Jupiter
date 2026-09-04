using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class UltraExecuteResponse
{
    public string? Status { get; set; }

    public string? Signature { get; set; }

    public string? Error { get; set; }

    public int? Code { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonIgnore]
    public bool IsSuccess => string.Equals(Status, "Success", StringComparison.Ordinal);
}
