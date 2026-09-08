using System.Text.Json;
using Microsoft.Extensions.Logging;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter;

public sealed class JupiterSwapClient : IDisposable
{
    private readonly JupiterSwapClientOptions _options;
    private readonly ApiTransport _transport;

    public JupiterSwapClient(JupiterSwapClientOptions options, HttpClient? httpClient = null, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _transport = new ApiTransport(httpClient, logger, "Jupiter", () => _options.MinRequestInterval, request =>
        {
            if (!string.IsNullOrEmpty(_options.ApiKey))
                request.Headers.Add("x-api-key", _options.ApiKey);
        });
    }

    public async Task<OrderResponse> GetOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new QueryBuilder();
        request.BuildQuery(query);

        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.BaseUrl, "/order", query.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<OrderResponse>(json);
    }

    public async Task<BuildResponse> GetBuildAsync(BuildRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new QueryBuilder();
        request.BuildQuery(query);

        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.BaseUrl, "/build", query.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<BuildResponse>(json);
    }

    public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var json = await SendAsync(HttpMethod.Post, BuildUrl(_options.BaseUrl, "/execute", null), body, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<ExecuteResponse>(json);
    }

    public async Task<IReadOnlyDictionary<string, TokenPrice>> GetPricesAsync(
        IEnumerable<string> mints,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mints);

        var query = new QueryBuilder();
        query.AddCsv("ids", mints.ToList());
        if (query.ToString().Length == 0)
            throw new ArgumentException("At least one mint is required.", nameof(mints));

        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.PriceApiUrl, null, query.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        var prices = JsonSerializer.Deserialize<Dictionary<string, TokenPrice>>(json, JsonDefaults.Options);
        return prices ?? new Dictionary<string, TokenPrice>();
    }

    public async Task<IReadOnlyList<TokenInfo>> SearchTokensAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        var builder = new QueryBuilder();
        builder.Add("query", query);

        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.TokensApiUrl, "/search", builder.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return DeserializeTokenList(json);
    }

    public async Task<IReadOnlyList<TokenInfo>> GetTokensByTagAsync(TokenTag tag, CancellationToken cancellationToken = default)
    {
        var builder = new QueryBuilder();
        builder.Add("query", TokenTagToString(tag));

        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.TokensApiUrl, "/tag", builder.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return DeserializeTokenList(json);
    }

    public async Task<IReadOnlyList<TokenInfo>> GetTopTokensAsync(
        TokenCategory category,
        TokenInterval interval,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit must be between 1 and 100.");

        var builder = new QueryBuilder();
        builder.Add("limit", limit);

        var path = "/" + TokenCategoryToString(category) + "/" + TokenIntervalToString(interval);
        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.TokensApiUrl, path, builder.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return DeserializeTokenList(json);
    }

    public async Task<IReadOnlyList<TokenInfo>> GetRecentTokensAsync(CancellationToken cancellationToken = default)
    {
        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.TokensApiUrl, "/recent", null), null, cancellationToken)
            .ConfigureAwait(false);
        return DeserializeTokenList(json);
    }

    public async Task<PortfolioResponse> GetPortfolioAsync(
        string walletAddress,
        IEnumerable<string>? platforms = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletAddress);

        var builder = new QueryBuilder();
        if (platforms is not null)
            builder.AddCsv("platforms", platforms.ToList());

        var path = "/positions/" + Uri.EscapeDataString(walletAddress);
        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.PortfolioApiUrl, path, builder.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<PortfolioResponse>(json);
    }

    public async Task<IReadOnlyList<PortfolioPlatform>> GetPlatformsAsync(CancellationToken cancellationToken = default)
    {
        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.PortfolioApiUrl, "/platforms", null), null, cancellationToken)
            .ConfigureAwait(false);
        var list = JsonSerializer.Deserialize<List<PortfolioPlatform>>(json, JsonDefaults.Options);
        return list ?? new List<PortfolioPlatform>();
    }

    public async Task<StakedJupResponse> GetStakedJupAsync(string walletAddress, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletAddress);

        var path = "/staked-jup/" + Uri.EscapeDataString(walletAddress);
        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.PortfolioApiUrl, path, null), null, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<StakedJupResponse>(json);
    }

    public async Task<UltraOrderResponse> GetUltraOrderAsync(UltraOrderRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = new QueryBuilder();
        request.BuildQuery(query);

        var json = await SendAsync(HttpMethod.Get, BuildUrl(_options.UltraApiUrl, "/order", query.ToString()), null, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<UltraOrderResponse>(json);
    }

    public async Task<UltraExecuteResponse> UltraExecuteAsync(UltraExecuteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var body = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var json = await SendAsync(HttpMethod.Post, BuildUrl(_options.UltraApiUrl, "/execute", null), body, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<UltraExecuteResponse>(json);
    }

    public void Dispose() => _transport.Dispose();

    private Task<string> SendAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken) =>
        _transport.SendAsync(method, url, jsonBody, cancellationToken);

    private static string BuildUrl(string baseUrl, string? path, string? query)
    {
        var url = baseUrl.TrimEnd('/');
        if (!string.IsNullOrEmpty(path))
            url += path;
        if (!string.IsNullOrEmpty(query))
            url += "?" + query;
        return url;
    }

    private static IReadOnlyList<TokenInfo> DeserializeTokenList(string json)
    {
        var list = JsonSerializer.Deserialize<List<TokenInfo>>(json, JsonDefaults.Options);
        return list ?? new List<TokenInfo>();
    }

    private static string TokenTagToString(TokenTag tag) => tag switch
    {
        TokenTag.Lst => "lst",
        TokenTag.Verified => "verified",
        TokenTag.Stocks => "stocks",
        _ => throw new ArgumentOutOfRangeException(nameof(tag))
    };

    private static string TokenCategoryToString(TokenCategory category) => category switch
    {
        TokenCategory.TopOrganicScore => "toporganicscore",
        TokenCategory.TopTraded => "toptraded",
        TokenCategory.TopTrending => "toptrending",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static string TokenIntervalToString(TokenInterval interval) => interval switch
    {
        TokenInterval.FiveMinutes => "5m",
        TokenInterval.OneHour => "1h",
        TokenInterval.SixHours => "6h",
        TokenInterval.TwentyFourHours => "24h",
        _ => throw new ArgumentOutOfRangeException(nameof(interval))
    };
}
