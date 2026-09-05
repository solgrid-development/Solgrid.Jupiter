using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class CraftDepositResponse
{
    public string? Transaction { get; set; }

    public string? RequestId { get; set; }

    public string? ReceiverAddress { get; set; }

    public string? Mint { get; set; }

    public string? Amount { get; set; }

    public int? TokenDecimals { get; set; }

    public string? InputTokenAccount { get; set; }

    public string? OutputTokenAccount { get; set; }

    public string? JlTokenAccount { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

    [JsonIgnore]
    public bool HasTransaction => !string.IsNullOrEmpty(Transaction);
}
