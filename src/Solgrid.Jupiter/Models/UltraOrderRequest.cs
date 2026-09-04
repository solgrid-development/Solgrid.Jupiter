using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter.Models;

public sealed class UltraOrderRequest
{
    public required string InputMint { get; init; }

    public required string OutputMint { get; init; }

    public required string Amount { get; init; }

    public string? Taker { get; init; }

    internal void BuildQuery(QueryBuilder query)
    {
        query.Add("inputMint", InputMint);
        query.Add("outputMint", OutputMint);
        query.Add("amount", Amount);
        query.Add("taker", Taker);
    }
}
