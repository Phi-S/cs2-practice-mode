using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.SwapTeamsFolder;

public class SwapTeamsService : Base
{
    private readonly CommandService _commandService;

    public SwapTeamsService(CommandService commandService)
    {
        _commandService = commandService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.SwapTeam,
            CommandActionSwapTeam,
            ArgOption.NoArgs("Swaps teams for all players in the server"),
            Permissions.Flags.SwapTeam);
        base.Load(plugin);
    }

    private ErrorOr<Success> CommandActionSwapTeam(CCSPlayerController player, CommandInfo commandInfo)
    {
        Server.ExecuteCommand("mp_swapteams 1");
        return Result.Success;
    }
}