using System.Text.Json;
using System.Text.Json.Serialization;
using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter.Models;

public sealed class BuildResponse
{
    public string? InputMint { get; set; }

    public string? OutputMint { get; set; }

    public string? InAmount { get; set; }

    public string? OutAmount { get; set; }

    public string? OtherAmountThreshold { get; set; }

    public string? SwapMode { get; set; }

    public int? SlippageBps { get; set; }

    public string? PriceImpactPct { get; set; }

    public List<RoutePlanStep>? RoutePlan { get; set; }

    public List<Instruction>? ComputeBudgetInstructions { get; set; }

    public List<Instruction>? SetupInstructions { get; set; }

    public Instruction? SwapInstruction { get; set; }

    public Instruction? CleanupInstruction { get; set; }

    public List<Instruction>? OtherInstructions { get; set; }

    public Instruction? TipInstruction { get; set; }

    public Dictionary<string, List<string>>? AddressesByLookupTableAddress { get; set; }

    public BlockhashWithMetadata? BlockhashWithMetadata { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class BlockhashWithMetadata
{
    [JsonConverter(typeof(NumberArrayToByteArrayConverter))]
    public byte[]? Blockhash { get; set; }

    public long? LastValidBlockHeight { get; set; }

    public FetchedAt? FetchedAt { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class FetchedAt
{
    [JsonPropertyName("secs_since_epoch")]
    public long SecsSinceEpoch { get; set; }

    [JsonPropertyName("nanos_since_epoch")]
    public long NanosSinceEpoch { get; set; }
}
