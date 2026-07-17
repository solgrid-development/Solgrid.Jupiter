using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class BuildRequestQueryTests
{
    private static BuildRequest Minimal() => new()
    {
        InputMint = "So11111111111111111111111111111111111111112",
        OutputMint = "EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v",
        Amount = "1000000",
        Taker = "GkwFnmMDvn3HGMpJpWBg8tgJxr3NxNvg3AXxvXVPbRGJ"
    };

    [Fact]
    public void RequiredParametersOnly()
    {
        var query = BuildQuery(Minimal());

        Assert.Equal(
            "inputMint=So11111111111111111111111111111111111111112" +
            "&outputMint=EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v" +
            "&amount=1000000" +
            "&taker=GkwFnmMDvn3HGMpJpWBg8tgJxr3NxNvg3AXxvXVPbRGJ",
            query);
    }

    [Fact]
    public void AllOptionalParametersAreIncluded()
    {
        var request = Minimal();
        request.SlippageBps = 75;
        request.Mode = Solgrid.Jupiter.BuildMode.Fast;
        request.Dexes = ["Raydium", "Orca"];
        request.PlatformFeeBps = 10;
        request.FeeAccount = "FEE_ACCOUNT";
        request.MaxAccounts = 32;
        request.Payer = "PAYER";
        request.WrapAndUnwrapSol = false;
        request.DestinationTokenAccount = "DEST_TOKEN";
        request.BlockhashSlotsToExpiry = 100;
        request.TipAmount = "1000000";
        request.ComputeUnitPricePercentile = 7500;
        request.ForJitoBundle = true;

        var query = BuildQuery(request);

        Assert.Contains("slippageBps=75", query);
        Assert.Contains("mode=fast", query);
        Assert.Contains("dexes=Raydium%2COrca", query);
        Assert.Contains("platformFeeBps=10", query);
        Assert.Contains("feeAccount=FEE_ACCOUNT", query);
        Assert.Contains("maxAccounts=32", query);
        Assert.Contains("payer=PAYER", query);
        Assert.Contains("wrapAndUnwrapSol=false", query);
        Assert.Contains("destinationTokenAccount=DEST_TOKEN", query);
        Assert.Contains("blockhashSlotsToExpiry=100", query);
        Assert.Contains("tipAmount=1000000", query);
        Assert.Contains("computeUnitPricePercentile=7500", query);
        Assert.Contains("forJitoBundle=true", query);
    }

    [Fact]
    public void RealTimeSlippageSendsRtse()
    {
        var request = Minimal();
        request.RtseSlippage = true;

        var query = BuildQuery(request);

        Assert.Contains("slippageBps=rtse", query);
    }

    [Fact]
    public void RtseAndNumericSlippageAreMutuallyExclusive()
    {
        var request = Minimal();
        request.SlippageBps = 50;
        request.RtseSlippage = true;

        Assert.Throws<ArgumentException>(() => BuildQuery(request));
    }

    [Fact]
    public void ComputeUnitPriceLevelSendsNamedLevel()
    {
        var request = Minimal();
        request.ComputeUnitPriceLevel = Solgrid.Jupiter.ComputeUnitPriceLevel.VeryHigh;

        var query = BuildQuery(request);

        Assert.Contains("computeUnitPricePercentile=veryHigh", query);
    }

    [Fact]
    public void ComputeUnitPriceLevelAndPercentileAreMutuallyExclusive()
    {
        var request = Minimal();
        request.ComputeUnitPricePercentile = 2500;
        request.ComputeUnitPriceLevel = Solgrid.Jupiter.ComputeUnitPriceLevel.VeryHigh;

        Assert.Throws<ArgumentException>(() => BuildQuery(request));
    }

    [Fact]
    public void EmptyTakerThrows()
    {
        var request = new BuildRequest
        {
            InputMint = "IN",
            OutputMint = "OUT",
            Amount = "1",
            Taker = ""
        };

        Assert.Throws<ArgumentException>(() => BuildQuery(request));
    }

    [Fact]
    public void DexesAndExcludeDexesAreMutuallyExclusive()
    {
        var request = Minimal();
        request.Dexes = ["Raydium"];
        request.ExcludeDexes = ["Orca"];

        Assert.Throws<ArgumentException>(() => BuildQuery(request));
    }

    [Fact]
    public void PlatformFeeRequiresFeeAccount()
    {
        var request = Minimal();
        request.PlatformFeeBps = 10;

        Assert.Throws<ArgumentException>(() => BuildQuery(request));
    }

    private static string BuildQuery(BuildRequest request)
    {
        var query = new QueryBuilder();
        request.BuildQuery(query);
        return query.ToString();
    }
}
