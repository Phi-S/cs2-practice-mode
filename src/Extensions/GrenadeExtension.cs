using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._1.GrenadeStorageFolder;
using Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;
using ErrorOr;

namespace Cs2PracticeMode.Extensions;

public static class GrenadeExtension
{
    public static void TeleportToThrowPosition(this Grenade grenade, CCSPlayerController player)
    {
        TeleportToThrowPosition(player, grenade.ThrowPosition, grenade.Angle);
    }

    public static void TeleportToThrowPosition(this GrenadeJsonModel grenade, CCSPlayerController player)
    {
        TeleportToThrowPosition(player, grenade.ThrowPosition.ToCsVector(), grenade.Angle.ToQAngle());
    }

    private static void TeleportToThrowPosition(CCSPlayerController player, Vector throwPosition, QAngle angle)
    {
        if (player.IsValid == false || player.PlayerPawn.Value is null)
        {
            return;
        }

        player.RemoveNoClip();
        player.PlayerPawn.Value.Teleport(
            throwPosition,
            angle,
            new Vector(0, 0, 0));
    }

    public static ErrorOr<Success> ThrowGrenade(this Grenade grenade, CCSPlayerController player)
    {
        return ThrowGrenade(player, grenade.Type, grenade.InitialPosition, grenade.Angle, grenade.Velocity);
    }

    public static ErrorOr<Success> ThrowGrenade(this GrenadeJsonModel grenade, CCSPlayerController player)
    {
        return ThrowGrenade(player, grenade.Type, grenade.InitialPosition.ToCsVector(), grenade.Angle.ToQAngle(),
            grenade.Velocity.ToCsVector());
    }

    private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>
        CSmokeGrenadeProjectileCreateFuncLinux =
            new(
                @"\x55\x4c\x89\xc1\x48\x89\xe5\x41\x57\x41\x56\x49\x89\xd6\x48\x89\xf2\x48\x89\xfe\x41\x55\x45\x89\xcd\x41\x54\x4d\x89\xc4\x53\x48\x83\xec\x28\x48\x89\x7d\xb8\x48");

    private static ErrorOr<Success> ThrowGrenade(
        CCSPlayerController player,
        GrenadeType_t grenadeType,
        Vector initialPosition,
        QAngle angle,
        Vector velocity)
    {
        if (player.IsValid == false)
        {
            return Errors.PlayerNullOrNotValid();
        }

        if (player.Pawn.Value is null || player.Pawn.IsValid == false)
        {
            return Errors.Fail("Player pawn not valid");
        }

        if (grenadeType == GrenadeType_t.GRENADE_TYPE_SMOKE)
        {
            CSmokeGrenadeProjectileCreateFuncLinux.Invoke(
                initialPosition.Handle,
                initialPosition.Handle,
                velocity.Handle,
                velocity.Handle,
                player.Pawn.Value.Handle,
                45,
                player.TeamNum
            );

            return Result.Success;
        }

        CBaseCSGrenadeProjectile? createdGrenade = null;
        if (grenadeType == GrenadeType_t.GRENADE_TYPE_FIRE)
        {
            createdGrenade = Utilities.CreateEntityByName<CMolotovProjectile>(DesignerNames.ProjectileMolotov);
        }
        else if (grenadeType == GrenadeType_t.GRENADE_TYPE_EXPLOSIVE)
        {
            createdGrenade = Utilities.CreateEntityByName<CHEGrenadeProjectile>(DesignerNames.ProjectileHe);
        }
        else if (grenadeType == GrenadeType_t.GRENADE_TYPE_FLASH)
        {
            createdGrenade = Utilities.CreateEntityByName<CFlashbangProjectile>(DesignerNames.ProjectileFlashbang);
        }

        if (createdGrenade == null)
        {
            return Errors.Fail("No valid grenade type found to throw");
        }

        createdGrenade.Elasticity = 0.33f;
        createdGrenade.IsLive = false;
        createdGrenade.DmgRadius = 350.0f;
        createdGrenade.Damage = 99.0f;

        createdGrenade.InitialPosition.X = initialPosition.X;
        createdGrenade.InitialPosition.Y = initialPosition.Y;
        createdGrenade.InitialPosition.Z = initialPosition.Z;

        createdGrenade.InitialVelocity.X = velocity.X;
        createdGrenade.InitialVelocity.Y = velocity.Y;
        createdGrenade.InitialVelocity.Z = velocity.Z;

        createdGrenade.Teleport(initialPosition, angle, velocity);
        createdGrenade.DispatchSpawn();

        createdGrenade.Globalname = "custom";
        createdGrenade.AcceptInput("FireUser1", player, player);
        createdGrenade.AcceptInput("InitializeSpawnFromWorld");
        createdGrenade.TeamNum = player.TeamNum;
        createdGrenade.Thrower.Raw = player.PlayerPawn.Raw;
        createdGrenade.OriginalThrower.Raw = player.PlayerPawn.Raw;
        createdGrenade.OwnerEntity.Raw = player.PlayerPawn.Raw;

        return Result.Success;
    }
}