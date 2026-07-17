using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class OrderRequestQueryTests
{
    [Fact]
    public void RequiredParametersOnly()
    {
        var request = new OrderRequest
        {
            InputMint = "So11111111111111111111111111111111111111112",
            OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
            Amount = "1000000"
        };

        var query = BuildQuery(request);

        Assert.Equal(
            "inputMint=So11111111111111111111111111111111111111112" +
            "&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v" +
            "&amount=1000000",
            query);
    }

    [Fact]
    public void AllOptionalParametersAreIncluded()
    {
        var request = new OrderRequest
        {
            InputMint = "IN",
            OutputMint = "OUT",
            Amount = "1",
            Taker = "TAKER",
            Receiver = "RECEIVER",
            SwapMode = Solgrid.Jupiter.SwapMode.ExactIn,
            SlippageBps = 50,
            ReferralAccount = "REFERRAL",
            ReferralFee = 60,
            Payer = "PAYER",
            PriorityFeeLamports = 1000,
            JitoTipLamports = 2000,
            BroadcastFeeType = Solgrid.Jupiter.BroadcastFeeType.MaxCap,
            ExcludeRouters = ["jupiterz", "dflow"],
            ExcludeDexes = ["Raydium", "Orca V2"]
        };

        var query = BuildQuery(request);

        Assert.Contains("taker=TAKER", query);
        Assert.Contains("receiver=RECEIVER", query);
        Assert.Contains("swapMode=ExactIn", query);
        Assert.Contains("slippageBps=50", query);
        Assert.Contains("referralAccount=REFERRAL", query);
        Assert.Contains("referralFee=60", query);
        Assert.Contains("payer=PAYER", query);
        Assert.Contains("priorityFeeLamports=1000", query);
        Assert.Contains("jitoTipLamports=2000", query);
        Assert.Contains("broadcastFeeType=maxCap", query);
        Assert.Contains("excludeRouters=jupiterz%2Cdflow", query);
        Assert.Contains("excludeDexes=Raydium%2COrca%20V2", query);
    }

    [Fact]
    public void UnsetParameterAreOmitted()
    {
        var request = new OrderRequest
        {
            InputMint = "IN",
            OutputMint = "OUT",
            Amount = "1",
            SlippageBps = 0
        };

        var query = BuildQuery(request);

        Assert.Contains("slippageBps=0", query);
        Assert.DoesNotContain("taker", query);
        Assert.DoesNotContain("payer", query);
        Assert.DoesNotContain("excludeRouters", query);
    }

    private static string BuildQuery(OrderRequest request)
    {
        var query = new QueryBuilder();
        request.BuildQuery(query);
        return query.ToString();
    }
}
