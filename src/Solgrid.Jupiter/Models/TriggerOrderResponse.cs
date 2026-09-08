using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerOrderResponse
{
    public string? Id { get; set; }

    public string? TxSignature { get; set; }

    public bool? DepositConfirmed { get; set; }

    // present on idempotent retries of create
    public string? Message { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
