using Solgrid.Jupiter.Internal;
using Solgrid.Jupiter.Models;

namespace Solgrid.Jupiter.Tests;

public class BuildResponseDeserializationTests
{
    private static BuildResponse LoadFixture() =>
        JsonDefaults.Deserialize<BuildResponse>(Fixtures.Read("build_response.json"));

    [Fact]
    public void MapsQuoteFields()
    {
        var response = LoadFixture();

        Assert.Equal("So11111111111111111111111111111111111111112", response.InputMint);
        Assert.Equal("EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v", response.OutputMint);
        Assert.Equal("1000000", response.InAmount);
        Assert.Equal("103896", response.OutAmount);
        Assert.Equal("103377", response.OtherAmountThreshold);
        Assert.Equal("ExactIn", response.SwapMode);
        Assert.Equal(50, response.SlippageBps);
        Assert.Equal(4, response.RoutePlan?.Count);
    }

    [Fact]
    public void MapsInstructions()
    {
        var response = LoadFixture();

        var computeBudget = Assert.Single(response.ComputeBudgetInstructions!);
        Assert.Equal("ComputeBudget111111111111111111111111111111", computeBudget.ProgramId);
        Assert.Empty(computeBudget.Accounts!);
        Assert.Equal("AzkSAAAAAAAA", computeBudget.Data);

        Assert.Equal(4, response.SetupInstructions?.Count);
        Assert.Equal("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL", response.SetupInstructions?[0].ProgramId);

        Assert.Equal("JUP6LkbZbjS1jKKwapdHNy74zcZ3tLUZoi5QNyVTaV4", response.SwapInstruction?.ProgramId);
        Assert.Equal(60, response.SwapInstruction?.Accounts?.Count);

        Assert.Equal("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA", response.CleanupInstruction?.ProgramId);
        Assert.Null(response.TipInstruction);
        Assert.Empty(response.OtherInstructions!);
    }

    [Fact]
    public void MapsAddressLookupTables()
    {
        var response = LoadFixture();

        Assert.NotNull(response.AddressesByLookupTableAddress);
        Assert.Equal(4, response.AddressesByLookupTableAddress.Count);
        Assert.Contains("HiiYP224a2pudkyUKGdLBh4REiCVKVjZFvxzXyZ34Brc", response.AddressesByLookupTableAddress.Keys);
    }

    [Fact]
    public void MapsBlockhashMetadata()
    {
        var response = LoadFixture();

        var metadata = response.BlockhashWithMetadata;
        Assert.NotNull(metadata);
        Assert.Equal(32, metadata.Blockhash?.Length);
        Assert.Equal(new byte[] { 21, 179, 22, 101 }, metadata.Blockhash?[..4]);
        Assert.Equal(422230315, metadata.LastValidBlockHeight);
        Assert.Equal(1788505613, metadata.FetchedAt?.SecsSinceEpoch);
        Assert.Equal(174787746, metadata.FetchedAt?.NanosSinceEpoch);
    }
}
