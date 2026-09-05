namespace Solgrid.Jupiter.Models;

public sealed class TriggerVerifyRequest
{
    public required TriggerChallengeType Type { get; init; }

    public required string WalletPubkey { get; init; }

    // base58 signature over the challenge string (Type = Message)
    public string? Signature { get; set; }

    // base64 signed challenge transaction, never submitted on-chain (Type = Transaction)
    public string? SignedTransaction { get; set; }
}
