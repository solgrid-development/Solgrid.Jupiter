namespace Solgrid.Jupiter;

public sealed class JupiterSwapClientOptions
{
    public string BaseUrl { get; set; } = "https://api.jup.ag/swap/v2";

    public string PriceApiUrl { get; set; } = "https://api.jup.ag/price/v3";

    public string TokensApiUrl { get; set; } = "https://api.jup.ag/tokens/v2";

    public string PortfolioApiUrl { get; set; } = "https://api.jup.ag/portfolio/v1";

    public string UltraApiUrl { get; set; } = "https://api.jup.ag/ultra/v1";

    public string? ApiKey { get; set; }

    public TimeSpan MinRequestInterval { get; set; } = TimeSpan.FromMilliseconds(2000);
}
