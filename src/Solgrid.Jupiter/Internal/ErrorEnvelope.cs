namespace Solgrid.Jupiter.Internal;

internal sealed class ErrorEnvelope
{
    public string? RequestId { get; set; }

    public string? Error { get; set; }

    public int? Code { get; set; }
}
