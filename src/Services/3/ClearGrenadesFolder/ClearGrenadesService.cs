using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services._3.ClearGrenadesFolder;

public class ClearGrenadesService : Base
{
    private readonly CommandService _commandService;
    private readonly LastThrownGrenadeService _lastThrownGrenadeService;
    private readonly ILogger<ClearGrenadesService> _logger;

    public ClearGrenadesService(ILogger<ClearGrenadesService> logger, CommandService commandService,
        LastThrownGrenadeService lastThrownGrenadeService)
    {
        _logger = logger;
        _commandService = commandService;
        _lastThrownGrenadeService = lastThrownGrenadeService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.ClearPlayerGrenades,
            CommandHandlerClear,
            ArgOption.NoArgs("Clear all grenades thrown by player"),
            Permissions.Flags.ClearPlayerGrenades);

        _commandService.RegisterCommand(ChatCommands.ClearAllGrenades,
            CommandHandlerClearAll,
            ArgOption.NoArgs("Clear all grenades on the map"),
            Permissions.Flags.ClearAllGrenades);

        base.Load(plugin);
    }

    private ErrorOr<Success> CommandHandlerClear(CCSPlayerController player, CommandInfo commandInfo)
    {
        ClearGrenades(player);
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerClearAll(CCSPlayerController player, CommandInfo commandInfo)
    {
        ClearGrenades(player, true);
        return Result.Success;
    }

    private void ClearGrenades(CCSPlayerController player, bool all = false)
    {
        var smokes = Utilities
            .FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>(DesignerNames.ProjectileSmoke)
            .ToList();
        var mollies = Utilities
            .FindAllEntitiesByDesignerName<CSmokeGrenadeProjectile>(DesignerNames.ProjectileMolotov)
            .ToList();

        foreach (var entity in smokes.Concat(mollies))
        {
            if (all)
            {
                entity.Remove();
                continue;
            }

            try
            {
                if (entity.Thrower.Value?.Controller.Value is null)
                {
                    _logger.LogWarning("Cant clear entity {EntityDesignerName}. Thrower is null", entity.DesignerName);
                    continue;
                }

                if (entity.Thrower.Value.Controller.Value.SteamID == player.SteamID)
                {
                    entity.Remove();
                }
            }
            catch (AccessViolationException)
            {
                // If a smoke is thrown artificially, the entity.Thrower.Value call thrown a AccessViolationException
                // Remove entity, if last thrown nade position equals the entity initial position
                var lastThrownGrenade = _lastThrownGrenadeService.GetLastThrownGrenade(player);
                if (lastThrownGrenade.IsError)
                {
                    continue;
                }

                if (entity.InitialPosition == lastThrownGrenade.Value.InitialPosition)
                {
                    entity.Remove();
                }
            }
        }

        // Fire will always be removed
        var infernos = Utilities.FindAllEntitiesByDesignerName<CInferno>("inferno").ToList();
        infernos.ForEach(inferno => inferno.Remove());
    }
}