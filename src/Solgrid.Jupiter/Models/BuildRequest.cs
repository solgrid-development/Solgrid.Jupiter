using System.Text.Json;
using System.Text.Json.Serialization;
using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter.Models;

public sealed class BuildRequest
{
    public required string InputMint { get; init; }

    public required string OutputMint { get; init; }

    public required string Amount { get; init; }

    public required string Taker { get; init; }

    public int? SlippageBps { get; set; }

    public bool RtseSlippage { get; set; }

    public BuildMode? Mode { get; set; }

    public IReadOnlyList<string>? Dexes { get; set; }

    public IReadOnlyList<string>? ExcludeDexes { get; set; }

    public int? PlatformFeeBps { get; set; }

    public string? FeeAccount { get; set; }

    public int? MaxAccounts { get; set; }

    public string? Payer { get; set; }

    public bool? WrapAndUnwrapSol { get; set; }

    public string? DestinationTokenAccount { get; set; }

    public string? NativeDestinationAccount { get; set; }

    public int? BlockhashSlotsToExpiry { get; set; }

    public string? TipAmount { get; set; }

    public int? ComputeUnitPricePercentile { get; set; }

    public ComputeUnitPriceLevel? ComputeUnitPriceLevel { get; set; }

    public bool? ForJitoBundle { get; set; }

    internal void BuildQuery(QueryBuilder query)
    {
        if (string.IsNullOrWhiteSpace(Taker))
            throw new ArgumentException("Taker is required for /build.", nameof(Taker));
        if (Dexes is { Count: > 0 } && ExcludeDexes is { Count: > 0 })
            throw new ArgumentException("Dexes and ExcludeDexes are mutually exclusive.");
        if (PlatformFeeBps is > 0 && string.IsNullOrWhiteSpace(FeeAccount))
            throw new ArgumentException("FeeAccount is required when PlatformFeeBps is positive.", nameof(FeeAccount));
        if (RtseSlippage && SlippageBps.HasValue)
            throw new ArgumentException("RtseSlippage and SlippageBps are mutually exclusive.");
        if (ComputeUnitPricePercentile.HasValue && ComputeUnitPriceLevel.HasValue)
            throw new ArgumentException("ComputeUnitPricePercentile and ComputeUnitPriceLevel are mutually exclusive.");

        query.Add("inputMint", InputMint);
        query.Add("outputMint", OutputMint);
        query.Add("amount", Amount);
        query.Add("taker", Taker);

        if (RtseSlippage)
            query.Add("slippageBps", "rtse");
        else
            query.Add("slippageBps", SlippageBps);

        if (Mode.HasValue)
            query.Add("mode", Mode.Value == BuildMode.Fast ? "fast" : Mode.Value.ToString());

        query.AddCsv("dexes", Dexes);
        query.AddCsv("excludeDexes", ExcludeDexes);
        query.Add("platformFeeBps", PlatformFeeBps);
        query.Add("feeAccount", FeeAccount);
        query.Add("maxAccounts", MaxAccounts);
        query.Add("payer", Payer);
        query.Add("wrapAndUnwrapSol", WrapAndUnwrapSol);
        query.Add("destinationTokenAccount", DestinationTokenAccount);
        query.Add("nativeDestinationAccount", NativeDestinationAccount);
        query.Add("blockhashSlotsToExpiry", BlockhashSlotsToExpiry);
        query.Add("tipAmount", TipAmount);

        if (ComputeUnitPriceLevel.HasValue)
            query.Add("computeUnitPricePercentile", ToQueryValue(ComputeUnitPriceLevel.Value));
        else
            query.Add("computeUnitPricePercentile", ComputeUnitPricePercentile);

        query.Add("forJitoBundle", ForJitoBundle);
    }

    private static string ToQueryValue(ComputeUnitPriceLevel level) => level switch
    {
        Solgrid.Jupiter.ComputeUnitPriceLevel.Medium => "medium",
        Solgrid.Jupiter.ComputeUnitPriceLevel.High => "high",
        Solgrid.Jupiter.ComputeUnitPriceLevel.VeryHigh => "veryHigh",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}
