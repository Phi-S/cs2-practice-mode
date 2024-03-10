using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Extensions;
using ErrorOr;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Cs2PracticeMode.Services.Last.LastThrownGrenadeFolder;

public class Grenade
{
    public required string Map { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required GrenadeType_t Type { get; set; }
    public List<string> Tags { get; set; } = new();
    public required ulong PlayerSteamId { get; set; }
    public required Vector ThrowPosition { get; set; }
    public required Vector InitialPosition { get; set; }
    public required QAngle Angle { get; set; }
    public required Vector Velocity { get; set; }
    public Vector? DetonationPosition { get; set; }

    public void RestorePosition(CCSPlayerController player)
    {
        if (player.IsValid == false || player.PlayerPawn.Value is null)
        {
            return;
        }

        player.RemoveNoClip();
        player.PlayerPawn.Value.Teleport(
            ThrowPosition,
            Angle,
            new Vector(0, 0, 0));
    }

    #region ThrowGrenade

    private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>
        CSmokeGrenadeProjectileCreateFuncWindows =
            new(
                @"\x48\x89\x5C\x24\x08\x48\x89\x6C\x24\x10\x48\x89\x74\x24\x18\x57\x41\x56\x41\x57\x48\x83\xEC\x50\x4C\x8B\xB4\x24\x90\x00\x00\x00\x49\x8B\xF8");

    private static readonly MemoryFunctionWithReturn<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, int>
        CSmokeGrenadeProjectileCreateFuncLinux =
            new(
                @"\x55\x4c\x89\xc1\x48\x89\xe5\x41\x57\x41\x56\x49\x89\xd6\x48\x89\xf2\x48\x89\xfe\x41\x55\x45\x89\xcd\x41\x54\x4d\x89\xc4\x53\x48\x83\xec\x28\x48\x89\x7d\xb8\x48");

    public ErrorOr<Success> ThrowGrenadeProjectile(CCSPlayerController player)
    {
        if (player.Pawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        CBaseCSGrenadeProjectile? cGrenade = null;
        switch (Type)
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
                        InitialPosition.Handle,
                        InitialPosition.Handle,
                        Velocity.Handle,
                        Velocity.Handle,
                        player.Pawn.Value.Handle,
                        45,
                        player.TeamNum
                    );
                }
                else if (OperatingSystem.IsWindows())
                {
                    CSmokeGrenadeProjectileCreateFuncWindows.Invoke(
                        InitialPosition.Handle,
                        InitialPosition.Handle,
                        Velocity.Handle,
                        Velocity.Handle,
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
            return Errors.Fail("No valid grenade found to throw");
        }

        cGrenade.InitialPosition.X = InitialPosition.X;
        cGrenade.InitialPosition.Y = InitialPosition.Y;
        cGrenade.InitialPosition.Z = InitialPosition.Z;
        cGrenade.InitialVelocity.X = Velocity.X;
        cGrenade.InitialVelocity.Y = Velocity.Y;
        cGrenade.InitialVelocity.Z = Velocity.Z;
        cGrenade.Teleport(
            InitialPosition,
            Angle,
            Velocity);

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

    #endregion
}