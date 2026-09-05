using System.Text.Json.Serialization;
using Solgrid.Jupiter.Internal;

namespace Solgrid.Jupiter;

public enum SwapMode
{
    ExactIn
}

public enum BroadcastFeeType
{
    MaxCap,
    ExactFee
}

public enum BuildMode
{
    Fast
}

public enum ComputeUnitPriceLevel
{
    Medium,
    High,
    VeryHigh
}

public enum TokenTag
{
    Lst,
    Verified,
    Stocks
}

public enum TokenCategory
{
    TopOrganicScore,
    TopTraded,
    TopTrending
}

public enum TokenInterval
{
    FiveMinutes,
    OneHour,
    SixHours,
    TwentyFourHours
}

[JsonConverter(typeof(SnakeCaseEnumConverter))]
public enum TriggerChallengeType
{
    Message,
    Transaction
}

[JsonConverter(typeof(SnakeCaseEnumConverter))]
public enum TriggerOrderType
{
    Single,
    Oco,
    Otoco
}

[JsonConverter(typeof(SnakeCaseEnumConverter))]
public enum TriggerDepositOrderType
{
    Price,
    Dca
}
