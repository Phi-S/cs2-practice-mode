using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.NoFlashFolder;

public class NoFlashService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;
    private readonly List<CCSPlayerController> _noFlashList = [];

    private readonly object _noFlashListLock = new();

    public NoFlashService(CommandService commandService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerBlind>(EventHandlerPlayerBlind);
        _commandService.RegisterCommand(ChatCommands.NoFlash,
            CommandHandlerNoFlash,
            ArgOption.NoArgs("Turns noflash on or of"),
            Permissions.Flags.NoFlash);
        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        lock (_noFlashListLock)
        {
            _noFlashList.Clear();
        }

        base.Unload(plugin);
    }

    /// <summary>
    ///     Lowest nade a player added
    /// </summary>
    private ErrorOr<Success> CommandHandlerNoFlash(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        lock (_noFlashList)
        {
            if (_noFlashList.Contains(player) == false)
            {
                _noFlashList.Add(player);
                _messagingService.MsgToPlayerCenter(player, "No flash enabled");
            }
            else
            {
                _noFlashList.Remove(player);
                _messagingService.MsgToPlayerCenter(player, "No flash disabled");
            }
        }

        return Result.Success;
    }

    private HookResult EventHandlerPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player is null || player.IsValid == false || player.PlayerPawn.Value is null)
        {
            return HookResult.Continue;
        }

        lock (_noFlashListLock)
        {
            if (_noFlashList.Contains(player))
            {
                player.PlayerPawn.Value.FlashMaxAlpha = 0.5f;
            }
        }

        return HookResult.Continue;
    }
}