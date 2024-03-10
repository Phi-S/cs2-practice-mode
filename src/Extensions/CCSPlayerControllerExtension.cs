using System.Drawing;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.SharedModels;

namespace Cs2PracticeMode.Extensions;

public static class CCSPlayerControllerExtension
{
    public static void TeleportToPosition(this CCSPlayerController player, Position position)
    {
        if (player.IsValid == false || player.PlayerPawn.Value == null)
        {
            return;
        }

        player.PlayerPawn.Value.Teleport(position.Pos, position.Angle, new Vector(0, 0, 0));
    }

    public static void RemoveNoClip(this CCSPlayerController player)
    {
        if (player.IsValid == false || player.PlayerPawn.IsValid == false || player.PlayerPawn.Value is null)
        {
            return;
        }

        if (player.PlayerPawn.Value.MoveType == MoveType_t.MOVETYPE_NOCLIP)
        {
            player.PlayerPawn.Value.MoveType = MoveType_t.MOVETYPE_WALK;
        }
    }

    public static Color GetTeamColor(this CCSPlayerController playerController)
    {
        switch (playerController.CompTeammateColor)
        {
            case 1:
                return Color.FromArgb(50, 255, 0);
            case 2:
                return Color.FromArgb(255, 255, 0);
            case 3:
                return Color.FromArgb(255, 132, 0);
            case 4:
                return Color.FromArgb(255, 0, 255);
            case 0:
                return Color.FromArgb(0, 187, 255);
            default:
                return Color.Red;
        }
    }
}