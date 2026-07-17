using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class ExecuteModelTests
{
    [Fact]
    public void SuccessResponse_MapsFields()
    {
        var response = JsonDefaults.Deserialize<ExecuteResponse>(Fixtures.Read("execute_success.json"));

        Assert.True(response.IsSuccess);
        Assert.Equal(0, response.Code);
        Assert.Equal("310456789", response.Slot);
        Assert.Equal("1000000", response.TotalInputAmount);
        Assert.Equal("103512", response.TotalOutputAmount);
        Assert.Equal("1000000", response.InputAmountResult);
        Assert.Equal("103616", response.OutputAmountResult);
        var swapEvent = Assert.Single(response.SwapEvents!);
        Assert.Equal("103616", swapEvent.OutputAmount);
    }

    [Fact]
    public void FailedResponse_MapsErrorFields()
    {
        var response = JsonDefaults.Deserialize<ExecuteResponse>(Fixtures.Read("execute_failed.json"));

        Assert.False(response.IsSuccess);
        Assert.Equal(-1000, response.Code);
        Assert.Equal("Failed to land", response.Error);
        Assert.NotNull(response.Signature);
    }

    [Fact]
    public void Request_SerializesOnlySetFields()
    {
        var request = new ExecuteRequest
        {
            SignedTransaction = "BASE64TX",
            RequestId = "req-42"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(request, JsonDefaults.Options);

        Assert.Equal("{\"signedTransaction\":\"BASE64TX\",\"requestId\":\"req-42\"}", json);
    }
}
