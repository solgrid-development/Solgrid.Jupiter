using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class AccountMeta
{
    public string? Pubkey { get; set; }

    public bool IsWritable { get; set; }

    public bool IsSigner { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
