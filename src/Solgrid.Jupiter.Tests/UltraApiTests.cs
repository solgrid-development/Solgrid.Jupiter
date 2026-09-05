using System.Net;
using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class UltraApiTests
{
    private const string UltraApiUrl = "https://unit.test/ultra/v1";
    private const string Wallet = "7HEeBYGMG6EWrT7WPsc3Ma1P1eu6cgVJC7DpCwPKGBse";

    private static JupiterSwapClientOptions NoThrottleOptions() => new()
    {
        BaseUrl = "https://unit.test/swap/v2",
        UltraApiUrl = UltraApiUrl,
        MinRequestInterval = TimeSpan.Zero
    };

    [Fact]
    public void UltraOrder_WithTaker_MapsLiveFixture()
    {
        var order = JsonDefaults.Deserialize<UltraOrderResponse>(Fixtures.Read("ultra_order_with_taker.json"));

        Assert.Equal("1000000", order.InAmount);
        Assert.Equal("103546", order.OutAmount);
        Assert.Equal("okx", order.Router);
        Assert.Equal("aggregator", order.SwapType);
        Assert.Equal("ultra", order.Mode);
        Assert.Equal(100, order.SlippageBps);
        Assert.Equal(2, order.FeeBps);
        Assert.Equal(Wallet, order.Taker);
        Assert.True(order.HasTransaction);
        Assert.Equal("422577737", order.LastValidBlockHeight);
        Assert.Equal(1855569, order.RentFeeLamports);
        Assert.Equal(5000, order.SignatureFeeLamports);
        Assert.Equal(66, order.TotalTime);
        Assert.False(order.Gasless);
        Assert.NotNull(order.RequestId);

        var step = Assert.Single(order.RoutePlan!);
        Assert.Equal(100, step.Percent);
        Assert.Equal(10000, step.Bps);
        Assert.Equal("OKX DEX Router", step.SwapInfo!.Label);
        Assert.Equal("103546", step.SwapInfo.OutAmount);
    }

    [Fact]
    public void UltraOrder_QuoteOnly_HasNoTransaction()
    {
        var order = JsonDefaults.Deserialize<UltraOrderResponse>(Fixtures.Read("ultra_order_quote_only.json"));

        Assert.Null(order.Transaction);
        Assert.False(order.HasTransaction);
        Assert.Null(order.Taker);
        Assert.Equal("metis", order.Router);
        Assert.NotNull(order.RequestId);
        Assert.True(order.OutUsdValue > 0);
    }

    [Fact]
    public async Task GetUltraOrder_BuildsUrlAndParsesResponse()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("ultra_order_with_taker.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var order = await client.GetUltraOrderAsync(new UltraOrderRequest
        {
            InputMint = "So11111111111111111111111111111111111111112",
            OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
            Amount = "1000000",
            Taker = Wallet
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            UltraApiUrl + "/order?inputMint=So11111111111111111111111111111111111111112" +
            "&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v&amount=1000000&taker=" + Wallet,
            request.RequestUri!.ToString());
        Assert.True(order.HasTransaction);
    }

    [Fact]
    public void UltraExecuteError_MapsCodeAndMessage()
    {
        var response = JsonDefaults.Deserialize<UltraExecuteResponse>(Fixtures.Read("ultra_execute_error.json"));

        Assert.Equal(-2, response.Code);
        Assert.Equal("Failed to decode signed transaction", response.Error);
        Assert.False(response.IsSuccess);
    }

    [Fact]
    public async Task UltraExecute_OnHttpError_ThrowsWithCode()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, Fixtures.Read("ultra_execute_error.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var ex = await Assert.ThrowsAsync<JupiterApiException>(() => client.UltraExecuteAsync(new UltraExecuteRequest
        {
            SignedTransaction = "AAAA",
            RequestId = "x"
        }));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(-2, ex.Code);
    }

    [Fact]
    public async Task UltraExecute_PostsCamelCaseBodyAndMapsSuccess()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "{\"status\":\"Success\",\"signature\":\"5Sig...\"}");
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var result = await client.UltraExecuteAsync(new UltraExecuteRequest
        {
            SignedTransaction = "AQID",
            RequestId = "req-1"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(UltraApiUrl + "/execute", request.RequestUri!.ToString());
        var body = Assert.Single(handler.RequestBodies);
        Assert.Contains("\"signedTransaction\":\"AQID\"", body);
        Assert.Contains("\"requestId\":\"req-1\"", body);
        Assert.True(result.IsSuccess);
        Assert.Equal("5Sig...", result.Signature);
    }
}
