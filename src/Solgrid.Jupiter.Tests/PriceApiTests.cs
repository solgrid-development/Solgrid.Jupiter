using System.Net;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class PriceApiTests
{
    private const string PriceApiUrl = "https://unit.test/price/v3";

    private static JupiterSwapClientOptions NoThrottleOptions() => new()
    {
        BaseUrl = "https://unit.test/swap/v2",
        PriceApiUrl = PriceApiUrl,
        MinRequestInterval = TimeSpan.Zero
    };

    [Fact]
    public void PriceResponse_MapsPricesByMint()
    {
        var prices = JsonDefaults.Deserialize<Dictionary<string, TokenPrice>>(Fixtures.Read("price_response.json"));

        Assert.Equal(3, prices.Count);

        var sol = prices["So11111111111111111111111111111111111111112"];
        Assert.Equal(103.93403606922196, sol.UsdPrice);
        Assert.Equal(9, sol.Decimals);
        Assert.Equal(444199789, sol.BlockId);
        Assert.True(sol.Liquidity > 800_000_000);

        var usdc = prices["EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"];
        Assert.Equal(6, usdc.Decimals);
        Assert.True(usdc.UsdPrice > 0.99 && usdc.UsdPrice < 1.01);

        var jup = prices["JUPyiwrYJFskUPiHa7hkeR8VUtAeFoSYbKedZNsDvCN"];
        Assert.True(jup.PriceChange24h < 0);
    }

    [Fact]
    public async Task GetPrices_BuildsUrlAndParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("price_response.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var prices = await client.GetPricesAsync(new[]
        {
            "So11111111111111111111111111111111111111112",
            "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            PriceApiUrl + "?ids=So11111111111111111111111111111111111111112%2CEPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
            request.RequestUri!.ToString());
        Assert.Equal(3, prices.Count);
    }

    [Fact]
    public async Task GetPrices_EmptyMintListThrows()
    {
        var handler = new FakeHttpHandler();
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        await Assert.ThrowsAsync<ArgumentException>(() => client.GetPricesAsync(Array.Empty<string>()));
        Assert.Empty(handler.Requests);
    }
}
