using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using Cs2PracticeMode.SharedModels;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.FlashFolder;

public class FlashService : Base
{
    private readonly CommandService _commandService;

    private readonly ConcurrentDictionary<CCSPlayerController, Position> _flashPosition = new();
    private readonly ConcurrentDictionary<CCSPlayerController, HtmlPrint> _htmlPrints = new();
    private readonly MessagingService _messagingService;

    public FlashService(CommandService commandService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnMapStart>(ListenerHandlerOnMapStart);
        plugin.RegisterListener<Listeners.OnEntitySpawned>(ListenerHandlerOnEntitySpawned);

        _commandService.RegisterCommand(ChatCommands.FlashMode,
            CommandHandlerFlash,
            ArgOption.NoArgs("Starts the flashing mode"),
            Permissions.Flags.FlashMode);

        _commandService.RegisterCommand(ChatCommands.StopFlashMode,
            CommandHandlerStop,
            ArgOption.NoArgs("Stops the flashing mode"),
            Permissions.Flags.FlashMode);

        base.Load(plugin);
    }

    private void ListenerHandlerOnMapStart(string _)
    {
        _flashPosition.Clear();
        foreach (var centerMessage in _htmlPrints)
        {
            _messagingService.HideCenterHtml(centerMessage.Key, centerMessage.Value);
        }
        _htmlPrints.Clear();
    }

    private ErrorOr<Success> CommandHandlerFlash(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        _flashPosition[player] = Position.CopyFrom(player.PlayerPawn.Value);

        var showCenterHtml =
            _messagingService.ShowCenterHtml(player, "In flashing mode. Use .stop to disable flashing mode");
        if (showCenterHtml.IsError)
        {
            return showCenterHtml.Errors;
        }

        _htmlPrints[player] = showCenterHtml.Value;

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerStop(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (_flashPosition.TryRemove(player, out _) == false)
        {
            _messagingService.MsgToPlayerChat(player, "Not in flashing mode");
        }
        else
        {
            _messagingService.MsgToPlayerCenter(player, "Stopped flashing mode");
        }

        if (_htmlPrints.TryRemove(player, out var htmlPrint))
        {
            _messagingService.HideCenterHtml(player, htmlPrint);
        }
        
        return Result.Success;
    }

    private void ListenerHandlerOnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName.Equals(DesignerNames.ProjectileFlashbang) == false)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            var flash = new CFlashbangProjectile(entity.Handle);
            if (flash.Thrower.Value?.Controller.Value is null)
            {
                return;
            }

            var player = new CCSPlayerController(flash.Thrower.Value.Controller.Value.Handle);
            if (player.IsValid == false)
            {
                return;
            }

            if (_flashPosition.TryGetValue(player, out var position) == false)
            {
                return;
            }

            player.PlayerPawn.Value!.Teleport(position.Pos, position.Angle, new Vector(0, 0, 0));
        });
    }
}