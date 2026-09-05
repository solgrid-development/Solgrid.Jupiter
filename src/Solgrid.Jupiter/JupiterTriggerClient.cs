using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter;

public sealed class JupiterTriggerClient : IDisposable
{
    private const int MaxLoggedBodyLength = 4000;
    private const string RateLimitResetHeader = "x-ratelimit-reset";

    private readonly JupiterTriggerClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _throttleGate = new(1, 1);
    private DateTimeOffset _lastRequestStartUtc = DateTimeOffset.MinValue;

    public JupiterTriggerClient(JupiterTriggerClientOptions options, HttpClient? httpClient = null, ILogger? logger = null)
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

    public async Task<TriggerChallengeResponse> GetChallengeAsync(
        string walletPubkey,
        TriggerChallengeType type = TriggerChallengeType.Message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletPubkey);

        var body = JsonSerializer.Serialize(
            new { walletPubkey, type = type == TriggerChallengeType.Transaction ? "transaction" : "message" },
            JsonDefaults.Options);

        var json = await SendAsync(HttpMethod.Post, BuildUrl("/auth/challenge", null), body, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<TriggerChallengeResponse>(json);
    }

    public async Task<TriggerVerifyResponse> VerifyAsync(TriggerVerifyRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Type == TriggerChallengeType.Message && string.IsNullOrEmpty(request.Signature))
            throw new ArgumentException("Signature is required to verify a message challenge.", nameof(request));
        if (request.Type == TriggerChallengeType.Transaction && string.IsNullOrEmpty(request.SignedTransaction))
            throw new ArgumentException("SignedTransaction is required to verify a transaction challenge.", nameof(request));

        var body = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var json = await SendAsync(HttpMethod.Post, BuildUrl("/auth/verify", null), body, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<TriggerVerifyResponse>(json);
    }

    public async Task<TriggerVaultResponse?> GetVaultAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            var json = await SendAsync(HttpMethod.Get, BuildUrl("/vault", null), null, cancellationToken)
                .ConfigureAwait(false);
            return JsonDefaults.Deserialize<TriggerVaultResponse>(json);
        }
        catch (JupiterApiException ex) when (ex.StatusCode == 404)
        {
            // no vault yet, caller should hit RegisterVaultAsync
            return null;
        }
    }

    public async Task<TriggerVaultResponse> RegisterVaultAsync(CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        var json = await SendAsync(HttpMethod.Get, BuildUrl("/vault/register", null), null, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<TriggerVaultResponse>(json);
    }

    public async Task<CraftDepositResponse> CraftDepositAsync(CraftDepositRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureAuthenticated();
        if (request.OrderType == TriggerDepositOrderType.Dca && request.OrderSubType.HasValue)
            throw new ArgumentException("OrderSubType must be null for dca deposits.", nameof(request));
        if (request.OrderType == TriggerDepositOrderType.Price && !request.OrderSubType.HasValue)
            throw new ArgumentException("OrderSubType is required for price deposits.", nameof(request));

        var body = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var json = await SendAsync(HttpMethod.Post, BuildUrl("/deposit/craft", null), body, cancellationToken)
            .ConfigureAwait(false);
        return JsonDefaults.Deserialize<CraftDepositResponse>(json);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _httpClient.Dispose();
        _throttleGate.Dispose();
    }

    private void EnsureAuthenticated()
    {
        if (string.IsNullOrEmpty(_options.AuthToken))
            throw new InvalidOperationException(
                "Trigger v2 requires a JWT: call GetChallengeAsync, sign it, then VerifyAsync, and set options.AuthToken.");
    }

    private string BuildUrl(string path, string? query)
    {
        var url = _options.BaseUrl.TrimEnd('/') + path;
        if (!string.IsNullOrEmpty(query))
            url += "?" + query;
        return url;
    }

    private async Task<string> SendAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        await ThrottleAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Jupiter trigger request: {Method} {Url}", method.Method, url);

        using var response = await SendWithRateLimitRetryAsync(method, url, jsonBody, cancellationToken)
            .ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Jupiter trigger response: {StatusCode} ({Length} chars): {Body}",
            (int)response.StatusCode,
            body.Length,
            Truncate(body));

        if (!response.IsSuccessStatusCode)
            throw JupiterApiException.FromResponse((int)response.StatusCode, body);

        return body;
    }

    private async Task<HttpResponseMessage> SendWithRateLimitRetryAsync(
        HttpMethod method,
        string url,
        string? jsonBody,
        CancellationToken cancellationToken)
    {
        var response = await SendCoreAsync(method, url, jsonBody, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode != HttpStatusCode.TooManyRequests)
            return response;

        var wait = GetRateLimitWait(response);
        response.Dispose();
        _logger.LogWarning("Jupiter trigger rate limit hit (429), retrying in {WaitSeconds:F1}s", wait.TotalSeconds);
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
        if (!string.IsNullOrEmpty(_options.AuthToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AuthToken);
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

        var seconds = reset > 1_000_000_000
            ? reset - DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            : reset;

        return TimeSpan.FromSeconds(Math.Clamp(seconds, 0.5, 10));
    }

    private async Task ThrottleAsync(CancellationToken cancellationToken)
    {
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

    private static string Truncate(string value) =>
        value.Length <= MaxLoggedBodyLength
            ? value
            : string.Concat(value.AsSpan(0, MaxLoggedBodyLength), "...");
}
