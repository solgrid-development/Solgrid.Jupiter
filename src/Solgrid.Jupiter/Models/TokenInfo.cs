using System.Text.Json;
using System.Text.Json.Serialization;

namespace Solgrid.Jupiter.Models;

public sealed class TokenInfo
{
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Symbol { get; set; }

    public string? Icon { get; set; }

    public int? Decimals { get; set; }

    public string? TokenProgram { get; set; }

    public string? CreatedAt { get; set; }

    public string? Twitter { get; set; }

    public string? Telegram { get; set; }

    public string? Website { get; set; }

    public string? Discord { get; set; }

    public string? Instagram { get; set; }

    public string? Tiktok { get; set; }

    public string? OtherUrl { get; set; }

    public string? Dev { get; set; }

    public string? MintAuthority { get; set; }

    public string? FreezeAuthority { get; set; }

    public double? CircSupply { get; set; }

    public double? TotalSupply { get; set; }

    public string? Launchpad { get; set; }

    public string? PartnerConfig { get; set; }

    public string? GraduatedPool { get; set; }

    public string? GraduatedAt { get; set; }

    public long? HolderCount { get; set; }

    public double? Fdv { get; set; }

    public double? Mcap { get; set; }

    public double? UsdPrice { get; set; }

    public long? PriceBlockId { get; set; }

    public double? Liquidity { get; set; }

    public TokenApy? Apy { get; set; }

    public TokenSwapStats? Stats5m { get; set; }

    public TokenSwapStats? Stats1h { get; set; }

    public TokenSwapStats? Stats6h { get; set; }

    public TokenSwapStats? Stats24h { get; set; }

    public TokenFirstPool? FirstPool { get; set; }

    public TokenAudit? Audit { get; set; }

    public double? OrganicScore { get; set; }

    public string? OrganicScoreLabel { get; set; }

    public bool? IsVerified { get; set; }

    public List<string>? Tags { get; set; }

    public string? UpdatedAt { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }
}
