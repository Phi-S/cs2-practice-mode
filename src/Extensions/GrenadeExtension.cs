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
        CSmokeGrenadeProjectileCreateFuncWindows =
            new(
                @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x48\x89\x74\x24\x18\x57\x41\x56\x41\x57\x48\x83\xEC\x50\x4C\x8B\xB4\x24\x90\x00\x00\x00\x49\x8B\xF8");

    private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>
        CSmokeGrenadeProjectileCreateFuncLinux =
            new(
                @"\x55\x4c\x89\xc1\x48\x89\xe5\x41\x57\x41\x56\x49\x89\xd6\x48\x89\xf2\x48\x89\xfe\x41\x55\x45\x89\xcd\x41\x54\x4d\x89\xc4\x53\x48\x83\xec\x28\x48\x89\x7d\xb8\x48");

    private static ErrorOr<Success> ThrowGrenade(
        CCSPlayerController player,
        GrenadeType_t grenadeType,
        Vector initialPosition,
        QAngle angle, Vector velocity)
    {
        if (player.IsValid == false)
        {
            return Errors.PlayerNullOrNotValid();
        }

        if (player.Pawn.Value is null || player.Pawn.IsValid == false)
        {
            return Errors.Fail("Player pawn not valid");
        }

        CBaseCSGrenadeProjectile? cGrenade = null;
        switch (grenadeType)
        {
            case GrenadeType_t.GRENADE_TYPE_EXPLOSIVE:
            {
                cGrenade = Utilities.CreateEntityByName<CHEGrenadeProjectile>(DesignerNames.ProjectileHe);
                if (cGrenade != null)
                {
                    cGrenade.Damage = 100;
                    cGrenade.DmgRadius = cGrenade.Damage * 3.5f;
                }

                break;
            }
            case GrenadeType_t.GRENADE_TYPE_FLASH:
            {
                cGrenade = Utilities.CreateEntityByName<CFlashbangProjectile>(DesignerNames.ProjectileFlashbang);
                break;
            }
            case GrenadeType_t.GRENADE_TYPE_SMOKE:
            {
                cGrenade = Utilities.CreateEntityByName<CSmokeGrenadeProjectile>(DesignerNames.ProjectileSmoke);
                cGrenade!.IsSmokeGrenade = true;
                if (OperatingSystem.IsLinux())
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
                }
                else if (OperatingSystem.IsWindows())
                {
                    CSmokeGrenadeProjectileCreateFuncWindows.Invoke(
                        initialPosition.Handle,
                        initialPosition.Handle,
                        velocity.Handle,
                        velocity.Handle,
                        player.Pawn.Value.Handle,
                        45,
                        player.TeamNum
                    );
                }
                else
                {
                    return Errors.Fail("Unknown operating system");
                }

                return Result.Success;
            }
            case GrenadeType_t.GRENADE_TYPE_FIRE:
            {
                cGrenade = Utilities.CreateEntityByName<CMolotovProjectile>(DesignerNames.ProjectileMolotov);
                if (cGrenade != null)
                {
                    cGrenade.SetModel("weapons/models/grenade/incendiary/weapon_incendiarygrenade.vmdl");
                }

                if (cGrenade != null)
                {
                    cGrenade.Damage = 200;
                    cGrenade.DmgRadius = 300;
                }

                break;
            }
            case GrenadeType_t.GRENADE_TYPE_DECOY:
            {
                cGrenade = Utilities.CreateEntityByName<CDecoyProjectile>(DesignerNames.ProjectileDecoy);
                break;
            }
            case GrenadeType_t.GRENADE_TYPE_SENSOR:
                break;
            case GrenadeType_t.GRENADE_TYPE_SNOWBALL:
                break;
            case GrenadeType_t.GRENADE_TYPE_TOTAL:
                break;
        }

        if (cGrenade == null)
        {
            return Errors.Fail("No valid grenade type found to throw");
        }

        cGrenade.InitialPosition.X = initialPosition.X;
        cGrenade.InitialPosition.Y = initialPosition.Y;
        cGrenade.InitialPosition.Z = initialPosition.Z;
        cGrenade.InitialVelocity.X = velocity.X;
        cGrenade.InitialVelocity.Y = velocity.Y;
        cGrenade.InitialVelocity.Z = velocity.Z;
        cGrenade.Teleport(
            initialPosition,
            angle,
            velocity);

        cGrenade.DispatchSpawn();
        cGrenade.Globalname = "custom";
        cGrenade.AcceptInput("FireUser1", player, player);
        cGrenade.AcceptInput("InitializeSpawnFromWorld");
        cGrenade.TeamNum = player.TeamNum;
        cGrenade.Thrower.Raw = player.PlayerPawn.Raw;
        cGrenade.OriginalThrower.Raw = player.PlayerPawn.Raw;
        cGrenade.OwnerEntity.Raw = player.PlayerPawn.Raw;

        return Result.Success;
    }
}