using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class OrderResponseDeserializationTests
{
    [Fact]
    public void QuoteOnlyResponse_MapsDocumentedFields()
    {
        var response = JsonDefaults.Deserialize<OrderResponse>(Fixtures.Read("order_quote_only.json"));

        Assert.Equal("ultra", response.Mode);
        Assert.Equal("ExactIn", response.SwapMode);
        Assert.Equal("So11111111111111111111111111111111111111112", response.InputMint);
        Assert.Equal("EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v", response.OutputMint);
        Assert.Equal("1000000", response.InAmount);
        Assert.Equal("103643", response.OutAmount);
        Assert.Equal("dflow", response.Router);
        Assert.Equal(2, response.FeeBps);
        Assert.Equal(2, response.PlatformFee?.FeeBps);
        Assert.Null(response.PlatformFee?.Amount);
        Assert.Equal(3, response.RoutePlan?.Count);
        Assert.Equal("Whirlpools", response.RoutePlan?[0].SwapInfo?.Label);
        Assert.Equal(10000, response.RoutePlan?[0].Bps);
        Assert.Null(response.Transaction);
        Assert.False(response.HasTransaction);
        Assert.Null(response.ErrorCode);
        Assert.Equal("01a06b19-555f-730e-ad1a-8cc0343f169c", response.RequestId);
        Assert.Equal(102, response.TotalTime);
    }

    [Fact]
    public void QuoteOnlyResponse_KeepsUnknownFieldsInAdditionalProperties()
    {
        var response = JsonDefaults.Deserialize<OrderResponse>(Fixtures.Read("order_quote_only.json"));

        Assert.NotNull(response.AdditionalProperties);
        Assert.True(response.AdditionalProperties.ContainsKey("guaranteedPrice"));
        Assert.True(response.AdditionalProperties.ContainsKey("jitOptimized"));
        Assert.False(response.AdditionalProperties.ContainsKey("outAmount"));
    }

    [Fact]
    public void BuildFailedResponse_ExposesErrorFields()
    {
        var response = JsonDefaults.Deserialize<OrderResponse>(Fixtures.Read("order_build_failed.json"));

        Assert.Equal("", response.Transaction);
        Assert.False(response.HasTransaction);
        Assert.Equal(1, response.ErrorCode);
        Assert.Equal("Insufficient funds", response.ErrorMessage);
        Assert.Equal("metis", response.Router);
    }
}
