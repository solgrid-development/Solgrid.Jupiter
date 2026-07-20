using System.Diagnostics;
using System.Net;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class JupiterSwapClientTests
{
    private const string BaseUrl = "https://unit.test/swap/v2";

    private static OrderRequest SampleRequest() => new()
    {
        InputMint = "So11111111111111111111111111111111111111112",
        OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
        Amount = "1000000",
        Taker = "GkwFnmMDvn3HGMpJpWBg8tgJxr3NxNvg3AXxvXVPbRGJ"
    };

    private static JupiterSwapClientOptions NoThrottleOptions(string? apiKey = null) => new()
    {
        BaseUrl = BaseUrl,
        ApiKey = apiKey,
        MinRequestInterval = TimeSpan.Zero
    };

    [Fact]
    public async Task GetOrder_BuildsExpectedUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        await client.GetOrderAsync(SampleRequest());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            BaseUrl + "/order" +
            "?inputMint=So11111111111111111111111111111111111111112" +
            "&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v" +
            "&amount=1000000" +
            "&taker=GkwFnmMDvn3HGMpJpWBg8tgJxr3NxNvg3AXxvXVPbRGJ",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetOrder_SendsApiKeyHeader_WhenConfigured()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        using var client = new JupiterSwapClient(NoThrottleOptions("jup_test_key"), new HttpClient(handler));

        await client.GetOrderAsync(SampleRequest());

        var request = Assert.Single(handler.Requests);
        Assert.Equal("jup_test_key", request.Headers.GetValues("x-api-key").Single());
    }

    [Fact]
    public async Task GetOrder_OmitsApiKeyHeader_WhenKeyless()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        await client.GetOrderAsync(SampleRequest());

        var request = Assert.Single(handler.Requests);
        Assert.False(request.Headers.Contains("x-api-key"));
    }

    [Fact]
    public async Task GetOrder_ParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("order_quote_only.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.GetOrderAsync(SampleRequest());

        Assert.Equal("103643", response.OutAmount);
        Assert.Equal("dflow", response.Router);
        Assert.False(response.HasTransaction);
    }

    [Fact]
    public async Task GetOrder_ThrowsJupiterApiException_On400()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, Fixtures.Read("error_400.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JupiterApiException>(() => client.GetOrderAsync(SampleRequest()));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal("Invalid input mint", exception.Error);
        Assert.Equal("req-1", exception.RequestId);
    }

    [Fact]
    public async Task GetOrder_RetriesOnce_AfterRateLimit()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.TooManyRequests, "{}", new Dictionary<string, string>
        {
            ["x-ratelimit-reset"] = "0.5"
        });
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("order_quote_only.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.GetOrderAsync(SampleRequest());

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("dflow", response.Router);
    }

    [Fact]
    public async Task Client_SpacesRequests_ByMinRequestInterval()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        handler.Enqueue(HttpStatusCode.OK, "{}");
        using var client = new JupiterSwapClient(
            new JupiterSwapClientOptions { BaseUrl = BaseUrl, MinRequestInterval = TimeSpan.FromMilliseconds(400) },
            new HttpClient(handler));

        var stopwatch = Stopwatch.StartNew();
        await client.GetOrderAsync(SampleRequest());
        await client.GetOrderAsync(SampleRequest());
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds >= 350, $"elapsed: {stopwatch.ElapsedMilliseconds}ms");
    }
}
