using System.Globalization;

namespace Solgrid.Jupiter.Internal;

internal sealed class QueryBuilder
{
    private readonly List<KeyValuePair<string, string>> _parameters = [];

    public void Add(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _parameters.Add(new(name, value));
    }

    public void Add(string name, int? value)
    {
        if (value.HasValue)
            _parameters.Add(new(name, value.Value.ToString(CultureInfo.InvariantCulture)));
    }

    public void Add(string name, long? value)
    {
        if (value.HasValue)
            _parameters.Add(new(name, value.Value.ToString(CultureInfo.InvariantCulture)));
    }

    public void Add(string name, bool? value)
    {
        if (value.HasValue)
            _parameters.Add(new(name, value.Value ? "true" : "false"));
    }

    public void AddCsv(string name, IReadOnlyList<string>? values)
    {
        if (values is { Count: > 0 })
            _parameters.Add(new(name, string.Join(',', values)));
    }

    public override string ToString() =>
        string.Join("&", _parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
}
