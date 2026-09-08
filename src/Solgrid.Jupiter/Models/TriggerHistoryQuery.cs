using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter.Models;

public sealed class TriggerHistoryQuery
{
    public TriggerHistoryState? State { get; set; }

    public string? Mint { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public TriggerHistorySort? Sort { get; set; }

    public TriggerSortDirection? Direction { get; set; }

    internal void BuildQuery(QueryBuilder query)
    {
        if (State.HasValue)
            query.Add("state", State.Value == TriggerHistoryState.Active ? "active" : "past");
        query.Add("mint", Mint);
        query.Add("limit", Limit);
        query.Add("offset", Offset);
        if (Sort.HasValue)
            query.Add("sort", Sort.Value switch
            {
                TriggerHistorySort.UpdatedAt => "updated_at",
                TriggerHistorySort.CreatedAt => "created_at",
                TriggerHistorySort.ExpiresAt => "expires_at",
                _ => throw new ArgumentOutOfRangeException(nameof(Sort))
            });
        if (Direction.HasValue)
            query.Add("dir", Direction.Value == TriggerSortDirection.Asc ? "asc" : "desc");
    }
}
