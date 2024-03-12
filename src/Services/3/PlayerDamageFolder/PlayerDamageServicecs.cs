using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Services._1.SettingsStorageFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;

namespace Cs2PracticeMode.Services._3.PlayerDamageFolder;

public class PlayerDamageService : Base
{
    private readonly MessagingService _messagingService;
    private readonly SettingsStorageService _settingsStorageService;

    public PlayerDamageService(
        SettingsStorageService settingsStorageService,
        MessagingService messagingService)
    {
        _settingsStorageService = settingsStorageService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        base.Load(plugin);
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (_settingsStorageService.Get().DisableDamagePrint)
        {
            return HookResult.Continue;
        }

        var attackerName = "";
        try
        {
            attackerName = @event.Attacker.PlayerName;
        }
        catch (AccessViolationException)
        {
            // If a grenade is thrown/rethrown with the plugin,
            // the event.Attacker call throws an AccessViolationException
            // Rethrown grenades wont print the Attacker
        }

        _messagingService.MsgToAll(
            $"{ChatColors.Red}{attackerName}{ChatColors.White} damaged {ChatColors.Blue}{@event.Userid.PlayerName}{ChatColors.White} for {ChatColors.Green}{@event.DmgHealth}{ChatColors.White}hp with {ChatColors.Green}{@event.Weapon}");
        return HookResult.Continue;
    }
}