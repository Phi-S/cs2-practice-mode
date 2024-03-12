using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Services._1.SettingsStorageFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;

namespace Cs2PracticeMode.Services._3.BlindTimeFolder;

public class BlindTimeService : Base
{
    private readonly MessagingService _messagingService;
    private readonly SettingsStorageService _settingsStorageService;

    public BlindTimeService(
        SettingsStorageService settingsStorageService,
        MessagingService messagingService)
    {
        _settingsStorageService = settingsStorageService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
        base.Load(plugin);
    }

    private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
    {
        if (_settingsStorageService.Get().DisableBlindTimePrint)
        {
            return HookResult.Continue;
        }

        _messagingService.MsgToAll(
            $"{ChatColors.Red}{@event.Attacker.PlayerName}{ChatColors.White} flashed {ChatColors.Blue}{@event.Userid.PlayerName}{ChatColors.White} for {ChatColors.Green}{@event.BlindDuration:0.00}s");
        return HookResult.Continue;
    }
}