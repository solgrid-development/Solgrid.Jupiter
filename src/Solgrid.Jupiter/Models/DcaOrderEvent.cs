using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class DcaOrderEvent
{
    // deposit, fill, withdrawal, cancelled
    public string? Type { get; set; }

    // ISO-8601 (unlike price order events, which use epoch milliseconds)
    public string? Timestamp { get; set; }

    public string? TxSignature { get; set; }

    public int? RoundNumber { get; set; }

    public string? InputAmount { get; set; }

    public string? OutputAmount { get; set; }

    // success, failed, rescheduled, pending
    public string? State { get; set; }

    public int? AttemptCount { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
