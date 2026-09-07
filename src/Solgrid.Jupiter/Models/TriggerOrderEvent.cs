using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerOrderEvent
{
    // deposit, fill, withdrawal, cancelled, expired
    public string? Type { get; set; }

    // epoch milliseconds
    public long? Timestamp { get; set; }

    public string? TxSignature { get; set; }

    // success, failed, pending
    public string? State { get; set; }

    public string? Mint { get; set; }

    public string? Amount { get; set; }

    // fill events
    public string? OutputMint { get; set; }

    public string? OutputAmount { get; set; }

    // take_profit, stop_loss, buy_above, buy_below
    public string? OrderContext { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
