namespace Solgrid.Jupiter.Models;

public sealed class UltraExecuteRequest
{
    public required string SignedTransaction { get; init; }

    public required string RequestId { get; init; }
}
