using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerPagination
{
    public long? Total { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
