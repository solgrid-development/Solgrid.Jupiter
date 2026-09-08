using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerCancelResponse
{
    public string? Id { get; set; }

    // unsigned withdrawal transaction, sign and send via confirm-cancel
    public string? Transaction { get; set; }

    public string? RequestId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
