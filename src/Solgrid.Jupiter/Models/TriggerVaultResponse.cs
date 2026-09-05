using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerVaultResponse
{
    public string? UserPubkey { get; set; }

    public string? VaultPubkey { get; set; }

    public string? PrivyVaultId { get; set; }

    public string? PrivyUserId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
