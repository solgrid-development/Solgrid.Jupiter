namespace Solgrid.Jupiter.Models;

public sealed class ExecuteRequest
{
    public required string SignedTransaction { get; init; }

    public required string RequestId { get; init; }

    public string? LastValidBlockHeight { get; init; }
}
