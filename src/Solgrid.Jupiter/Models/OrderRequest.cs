using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter.Models;

public sealed class OrderRequest
{
    public required string InputMint { get; init; }

    public required string OutputMint { get; init; }

    public required string Amount { get; init; }

    public string? Taker { get; set; }

    public string? Receiver { get; set; }

    public SwapMode? SwapMode { get; set; }

    public int? SlippageBps { get; set; }

    public string? ReferralAccount { get; set; }

    public int? ReferralFee { get; set; }

    public string? Payer { get; set; }

    public long? PriorityFeeLamports { get; set; }

    public long? JitoTipLamports { get; set; }

    public BroadcastFeeType? BroadcastFeeType { get; set; }

    public IReadOnlyList<string>? ExcludeRouters { get; set; }

    public IReadOnlyList<string>? ExcludeDexes { get; set; }

    internal void BuildQuery(QueryBuilder query)
    {
        query.Add("inputMint", InputMint);
        query.Add("outputMint", OutputMint);
        query.Add("amount", Amount);
        query.Add("taker", Taker);
        query.Add("receiver", Receiver);
        if (SwapMode.HasValue)
            query.Add("swapMode", SwapMode.Value.ToString());
        query.Add("slippageBps", SlippageBps);
        query.Add("referralAccount", ReferralAccount);
        query.Add("referralFee", ReferralFee);
        query.Add("payer", Payer);
        query.Add("priorityFeeLamports", PriorityFeeLamports);
        query.Add("jitoTipLamports", JitoTipLamports);
        if (BroadcastFeeType.HasValue)
            query.Add("broadcastFeeType", BroadcastFeeType.Value == Solgrid.Jupiter.BroadcastFeeType.MaxCap ? "maxCap" : "exactFee");
        query.AddCsv("excludeRouters", ExcludeRouters);
        query.AddCsv("excludeDexes", ExcludeDexes);
    }
}
