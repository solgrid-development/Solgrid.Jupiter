using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter;

public sealed class JupiterSwapClient : IDisposable
{
    private const int MaxLoggedBodyLength = 4000;
    private const string RateLimitResetHeader = "x-ratelimit-reset";

    private readonly JupiterSwapClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _throttleGate = new(1, 1);
    private DateTimeOffset _lastRequestStartUtc = DateTimeOffset.MinValue;

    public JupiterSwapClient(JupiterSwapClientOptions options, HttpClient? httpClient = null, ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (httpClient is null)
        {
            _httpClient = new HttpClient();
            _ownsHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
        }

        _logger = logger ?? NullLogger.Instance;
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

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
        _throttleGate.Dispose();
    }

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

    private async Task<string> SendAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        await ThrottleAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Jupiter request: {Method} {Url}", method.Method, url);

        using var response = await SendWithRateLimitRetryAsync(method, url, jsonBody, cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Jupiter response: {StatusCode} ({Length} chars): {Body}",
            (int)response.StatusCode,
            body.Length,
            Truncate(body));

        if (!response.IsSuccessStatusCode)
            throw CreateApiException(response.StatusCode, body);

        return body;
    }

    private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        // TODO: honor Retry-After once the API starts sending it consistently
        var response = await SendCoreAsync(method, url, jsonBody, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
            return response;

        var wait = GetRateLimitWait(response);
        response.Dispose();
        _logger.LogWarning("Jupiter rate limit hit (429), retrying in {WaitSeconds:F1}s", wait.TotalSeconds);
        await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
        return await SendCoreAsync(method, url, jsonBody, cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(_options.ApiKey))
            request.Headers.Add("x-api-key", _options.ApiKey);
        if (jsonBody is not null)
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private TimeSpan GetRateLimitWait(HttpResponseMessage response)
    {
        var fallback = TimeSpan.FromSeconds(2);
        if (!response.Headers.TryGetValues(RateLimitResetHeader, out var values))
            return fallback;

        var raw = values.FirstOrDefault();
        if (raw is null || !double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var reset))
            return fallback;

        // x-ratelimit-reset arrives as epoch seconds from the swap endpoints and
        // as a delta from price; the magnitude check covers both (ugly but works)
        var seconds = reset > 1_000_000_000
            ? reset - DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            : reset;

        return TimeSpan.FromSeconds(Math.Clamp(seconds, 0.5, 10));
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
        // tried a sliding-window request queue here first, overkill for 0.5-1 RPS
        var interval = _options.MinRequestInterval;
        if (interval <= TimeSpan.Zero)
            return;

        await _throttleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var elapsed = DateTimeOffset.UtcNow - _lastRequestStartUtc;
            if (elapsed < interval)
                await Task.Delay(interval - elapsed, cancellationToken).ConfigureAwait(false);
            _lastRequestStartUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            _throttleGate.Release();
        }
    }

    private JupiterApiException CreateApiException(HttpStatusCode statusCode, string body)
    {
        string? error = null;
        string? requestId = null;
        int? code = null;
        try
        {
            var envelope = JsonSerializer.Deserialize<ErrorEnvelope>(body, JsonDefaults.Options);
            error = envelope?.Error;
            requestId = envelope?.RequestId;
            code = envelope?.Code;
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "error body was not a Jupiter error envelope");
        }

        return new JupiterApiException((int)statusCode, error ?? body, requestId, code, body);
    }

    private static string Truncate(string value) =>
        value.Length <= MaxLoggedBodyLength
            ? value
            : string.Concat(value.AsSpan(0, MaxLoggedBodyLength), "...");
}
