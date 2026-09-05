using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Internal;

// trigger v2 payloads use snake_case enum strings ("message", later "time_based"),
// STJ would write numbers by default
internal sealed class SnakeCaseEnumConverter : JsonStringEnumConverter
{
    public SnakeCaseEnumConverter() : base(JsonNamingPolicy.SnakeCaseLower)
    {
    }
}
