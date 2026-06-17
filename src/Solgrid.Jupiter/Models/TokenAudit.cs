using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TokenAudit
{
    public bool? IsSus { get; set; }

    public bool? MintAuthorityDisabled { get; set; }

    public bool? FreezeAuthorityDisabled { get; set; }

    public double? TopHoldersPercentage { get; set; }

    public double? DevBalancePercentage { get; set; }

    public long? DevMints { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
