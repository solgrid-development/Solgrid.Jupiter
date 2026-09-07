using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerTxSignatureResponse
{
    public string? Id { get; set; }

    public string? TxSignature { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
