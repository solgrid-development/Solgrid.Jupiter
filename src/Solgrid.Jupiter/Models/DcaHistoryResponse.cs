using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class DcaHistoryResponse
{
    public List<DcaHistoryItem>? Orders { get; set; }

    public TriggerPagination? Pagination { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
