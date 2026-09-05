namespace Solgrid.Jupiter;

public sealed class JupiterTriggerClientOptions
{
    public string BaseUrl { get; set; } = "https://api.jup.ag/trigger/v2";

    public string? ApiKey { get; set; }

    // JWT from VerifyAsync, valid 24h; there is no refresh endpoint,
    // re-run the challenge flow when it expires
    public string? AuthToken { get; set; }

    public TimeSpan MinRequestInterval { get; set; } = TimeSpan.FromMilliseconds(2000);
}
