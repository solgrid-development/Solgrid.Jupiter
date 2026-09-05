namespace Solgrid.Jupiter.Models;

public sealed class CraftDepositRequest
{
    public required string InputMint { get; init; }

    public required string OutputMint { get; init; }

    public required string UserAddress { get; init; }

    public required string Amount { get; init; }

    public required TriggerDepositOrderType OrderType { get; init; }

    // required for price orders, must stay null for dca
    public TriggerOrderType? OrderSubType { get; set; }

    // Earn While You Wait dca only: Jupiter Lend earn token for the input stablecoin
    public string? JlMint { get; set; }
}
