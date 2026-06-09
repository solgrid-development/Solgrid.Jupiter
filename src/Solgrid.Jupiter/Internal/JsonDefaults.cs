using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Internal;

internal static class JsonDefaults
{
    internal static readonly JsonSerializerOptions Options = CreateOptions();

    internal static T Deserialize<T>(string json) where T : class
    {
        var result = JsonSerializer.Deserialize<T>(json, Options);
        return result ?? throw new JupiterApiException(200, "Response deserialized to null", null, null, json);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }
}
