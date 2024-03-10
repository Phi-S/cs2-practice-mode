using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using ErrorOr;

namespace Cs2PracticeMode.Extensions;

public static class EntityExtension
{
    public static ErrorOr<GrenadeType_t> GetGrenadeType(this CEntityInstance projectile)
    {
        return projectile.DesignerName switch
        {
            DesignerNames.ProjectileSmoke => GrenadeType_t.GRENADE_TYPE_SMOKE,
            DesignerNames.ProjectileFlashbang => GrenadeType_t.GRENADE_TYPE_FLASH,
            DesignerNames.ProjectileHe => GrenadeType_t.GRENADE_TYPE_EXPLOSIVE,
            DesignerNames.ProjectileMolotov => GrenadeType_t.GRENADE_TYPE_FIRE,
            DesignerNames.ProjectileDecoy => GrenadeType_t.GRENADE_TYPE_DECOY,
            _ => Errors.Fail($"No grenade type fround for designer name \"{projectile.DesignerName}\"")
        };
    }
}