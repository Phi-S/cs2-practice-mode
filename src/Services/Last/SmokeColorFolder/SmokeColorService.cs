using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Extensions;
using Cs2PracticeMode.Services.Second.SettingsStorageFolder;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services.Last.SmokeColorFolder;

public class SmokeColorService : Base
{
    private readonly ILogger<SmokeColorService> _logger;
    private readonly SettingsStorageService _settingsStorageService;

    public SmokeColorService(ILogger<SmokeColorService> logger, SettingsStorageService settingsStorageService)
    {
        _logger = logger;
        _settingsStorageService = settingsStorageService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        base.Load(plugin);
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName.Equals(DesignerNames.ProjectileSmoke) == false)
        {
            return;
        }

        if (_settingsStorageService.Get().DisableSmokeColors)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            var smokeGrenadeProjectile = new CSmokeGrenadeProjectile(entity.Handle);
            if (smokeGrenadeProjectile.Thrower.Value?.Controller.Value is null)
            {
                _logger.LogError("Failed to set smoke color. Thrower not found");
                return;
            }

            var player = new CCSPlayerController(smokeGrenadeProjectile.Thrower.Value.Controller.Value.Handle);
            var playerColor = player.GetTeamColor();

            smokeGrenadeProjectile.SmokeColor.X = playerColor.R;
            smokeGrenadeProjectile.SmokeColor.Y = playerColor.G;
        });
    }
}