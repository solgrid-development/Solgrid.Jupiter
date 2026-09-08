using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Solgrid.Jupiter.Internal;

// shared send loop for both clients: throttle, one 429 retry, request/response
// logging. url building and request validation stay in the clients
internal sealed class ApiTransport : IDisposable
{
    private const int MaxLoggedBodyLength = 4000;
    private const string RateLimitResetHeader = "x-ratelimit-reset";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ILogger _logger;
    private readonly string _label;
    private readonly Func<TimeSpan> _minInterval;
    private readonly Action<HttpRequestMessage>? _decorate;
    private readonly SemaphoreSlim _throttleGate = new(1, 1);
    private DateTimeOffset _lastRequestStartUtc = DateTimeOffset.MinValue;

    public ApiTransport(
        HttpClient? httpClient,
        ILogger? logger,
        string label,
        Func<TimeSpan> minInterval,
        Action<HttpRequestMessage>? decorate = null)
    {
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
        _label = label;
        _minInterval = minInterval;
        _decorate = decorate;
    }

    public async Task<string> SendAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        await ThrottleAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("{Label} request: {Method} {Url}", _label, method.Method, url);

        using var response = await SendWithRateLimitRetryAsync(method, url, jsonBody, cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "{Label} response: {StatusCode} ({Length} chars): {Body}",
            _label,
            (int)response.StatusCode,
            body.Length,
            Truncate(body));

        if (!response.IsSuccessStatusCode)
            throw JupiterApiException.FromResponse((int)response.StatusCode, body);

        return body;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
        _throttleGate.Dispose();
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
        _logger.LogWarning("{Label} rate limit hit (429), retrying in {WaitSeconds:F1}s", _label, wait.TotalSeconds);
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
        _decorate?.Invoke(request);
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
        var interval = _minInterval();
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

    private static string Truncate(string value) =>
        value.Length <= MaxLoggedBodyLength
            ? value
            : string.Concat(value.AsSpan(0, MaxLoggedBodyLength), "...");
}
