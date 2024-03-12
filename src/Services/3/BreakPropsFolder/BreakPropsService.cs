using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.BreakPropsFolder;

public class BreakPropsService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;

    public BreakPropsService(CommandService commandService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.BreakProps,
            CommandHandlerBreakStuff,
            ArgOption.NoArgs("Break all breakable props on the map"),
            Permissions.Flags.Break);
        base.Load(plugin);
    }

    private ErrorOr<Success> CommandHandlerBreakStuff(CCSPlayerController player, CommandInfo commandInfo)
    {
        var props = Utilities.FindAllEntitiesByDesignerName<CBreakable>("prop_dynamic")
            .Concat(Utilities.FindAllEntitiesByDesignerName<CBreakable>("func_breakable"));
        foreach (var prop in props)
        {
            if (prop.IsValid)
            {
                prop.AcceptInput("break");
            }
        }

        _messagingService.MsgToPlayerChat(player, "Broke all breakable props on the map");
        return Result.Success;
    }
}