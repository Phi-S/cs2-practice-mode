using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Cs2PracticeMode.SharedModels;

public record Position(Vector Pos, QAngle Angle)
{
    public static Position CopyFrom(CCSPlayerPawn playerPawn)
    {
        var playerPosition = playerPawn.AbsOrigin;
        var playerAngle = playerPawn.EyeAngles;
        var savedPosition = new Vector(playerPosition?.X, playerPosition?.Y, playerPosition?.Z);
        var savedAngle = new QAngle(playerAngle.X, playerAngle.Y, playerAngle.Z);
        return new Position(savedPosition, savedAngle);
    }

    public float AbsolutDistance(Position position)
    {
        var distanceX = Pos.X - position.Pos.X;
        var distanceY = Pos.Y - position.Pos.Y;
        var distanceZ = Pos.Z - position.Pos.Z;
        if (distanceX < 0)
        {
            distanceX *= -1;
        }

        if (distanceY < 0)
        {
            distanceY *= -1;
        }

        if (distanceZ < 0)
        {
            distanceZ *= -1;
        }

        return distanceX + distanceY + distanceZ;
    }
}