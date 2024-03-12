using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;

public class Grenade
{
    public required GrenadeType_t Type { get; init; }
    public required Vector ThrowPosition { get; init; }
    public required Vector InitialPosition { get; init; }
    public required QAngle Angle { get; init; }
    public required Vector Velocity { get; init; }
    public Vector? DetonationPosition { get; set; }
}