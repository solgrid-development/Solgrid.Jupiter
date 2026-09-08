using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter.Models;

public sealed class DcaHistoryQuery
{
    public TriggerHistoryState? State { get; set; }

    public string? Mint { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public DcaHistorySort? Sort { get; set; }

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
                DcaHistorySort.UpdatedAt => "updated_at",
                DcaHistorySort.CreatedAt => "created_at",
                DcaHistorySort.NextFillAt => "next_fill_at",
                _ => throw new ArgumentOutOfRangeException(nameof(Sort))
            });
        if (Direction.HasValue)
            query.Add("dir", Direction.Value == TriggerSortDirection.Asc ? "asc" : "desc");
    }
}
