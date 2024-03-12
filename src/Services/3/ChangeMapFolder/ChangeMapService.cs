using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.ChangeMapFolder;

public class ChangeMapService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;

    public ChangeMapService(CommandService commandService, MessagingService messagingService)
    {
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.ChangeMap,
            CommandActionChangeMap,
            ArgOption.String("Change map", "map"),
            Permissions.Flags.ChangeMap);

        base.Load(plugin);
    }

    private ErrorOr<Success> CommandActionChangeMap(CCSPlayerController player, CommandInfo commandInfo)
    {
        var arg = commandInfo.GetArgString();
        if (arg.IsError)
        {
            return arg.FirstError;
        }

        var mapName = arg.Value.ToLower();
        if (Server.IsMapValid(mapName) == false)
        {
            return Errors.Fail($"Map \"{mapName}\" not found");
        }

        _messagingService.MsgToAll($"Changing map to {mapName}");
        Server.ExecuteCommand($"changelevel {mapName}");
        return Result.Success;
    }
}