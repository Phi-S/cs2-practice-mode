using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._0.PluginConfigFolder;

namespace Cs2PracticeMode.Services._3.PlayerPermissionsFolder;

public class SetPlayerPermissionsService : Base
{
    private readonly PluginConfigService _pluginConfigService;

    public SetPlayerPermissionsService(
        PluginConfigService pluginConfigService)
    {
        _pluginConfigService = pluginConfigService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerConnectFull>(EventHandlerOnPlayerConnectFull);
        base.Load(plugin);
    }

    private HookResult EventHandlerOnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (_pluginConfigService.Config.EnablePermissions == false)
        {
            AdminManager.AddPlayerPermissions(@event.Userid, Permissions.Flags.Root);
        }

        return HookResult.Continue;
    }
}