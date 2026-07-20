using System.Net;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class BuildAndExecuteClientTests
{
    private const string BaseUrl = "https://unit.test/swap/v2";

    private static JupiterSwapClientOptions NoThrottleOptions() => new()
    {
        BaseUrl = BaseUrl,
        MinRequestInterval = TimeSpan.Zero
    };

    private static BuildRequest SampleBuildRequest() => new()
    {
        InputMint = "So11111111111111111111111111111111111111112",
        OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
        Amount = "1000000",
        Taker = "GkwFnmMDvn3HGMpJpWBg8tgJxr3NxNvg3AXxvXVPbRGJ"
    };

    [Fact]
    public async Task GetBuild_BuildsExpectedUrl()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, "{}");
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        await client.GetBuildAsync(SampleBuildRequest());

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            BaseUrl + "/build" +
            "?inputMint=So11111111111111111111111111111111111111112" +
            "&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v" +
            "&amount=1000000" +
            "&taker=GkwFnmMDvn3HGMpJpWBg8tgJxr3NxNvg3AXxvXVPbRGJ",
            request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetBuild_ParsesLiveFixture()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("build_response.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.GetBuildAsync(SampleBuildRequest());

        Assert.Equal("103896", response.OutAmount);
        Assert.Equal("JUP6LkbZbjS1jKKwapdHNy74zcZ3tLUZoi5QNyVTaV4", response.SwapInstruction?.ProgramId);
        Assert.Equal(32, response.BlockhashWithMetadata?.Blockhash?.Length);
        Assert.Equal(4, response.AddressesByLookupTableAddress?.Count);
    }

    [Fact]
    public async Task Execute_PostsJsonBody()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.OK, Fixtures.Read("execute_success.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var response = await client.ExecuteAsync(new ExecuteRequest
        {
            SignedTransaction = "BASE64TX",
            RequestId = "req-42",
            LastValidBlockHeight = "422230315"
        });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(BaseUrl + "/execute", request.RequestUri!.ToString());
        Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal(
            "{\"signedTransaction\":\"BASE64TX\",\"requestId\":\"req-42\",\"lastValidBlockHeight\":\"422230315\"}",
            handler.RequestBodies.Single());
        Assert.True(response.IsSuccess);
    }

    [Fact]
    public async Task Execute_ThrowsWithCode_On400()
    {
        var handler = new FakeHttpHandler();
        handler.Enqueue(HttpStatusCode.BadRequest, Fixtures.Read("error_400_with_code.json"));
        using var client = new JupiterSwapClient(NoThrottleOptions(), new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<JupiterApiException>(() => client.ExecuteAsync(new ExecuteRequest
        {
            SignedTransaction = "BASE64TX",
            RequestId = "unknown"
        }));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal(-1, exception.Code);
        Assert.Equal("Invalid requestId", exception.Error);
    }
}
