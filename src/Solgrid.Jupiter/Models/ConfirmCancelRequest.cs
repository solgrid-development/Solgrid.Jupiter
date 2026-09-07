namespace Solgrid.Jupiter.Models;

public sealed class ConfirmCancelRequest
{
    public required string SignedTransaction { get; init; }

    public required string CancelRequestId { get; init; }
}
