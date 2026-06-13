using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class Instruction
{
    public string? ProgramId { get; set; }

    public List<InstructionAccount>? Accounts { get; set; }

    public string? Data { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class InstructionAccount
{
    public string? Pubkey { get; set; }

    public bool IsWritable { get; set; }

    public bool IsSigner { get; set; }
}
