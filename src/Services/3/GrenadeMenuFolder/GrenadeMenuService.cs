using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Extensions;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using Cs2PracticeMode.Services._1.GrenadeStorageFolder;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._3.GrenadeMenuFolder;

public partial class GrenadeMenuService : Base
{
    private readonly CommandService _commandService;
    private readonly GrenadeStorageService _grenadeStorageService;
    private readonly LastThrownGrenadeService _lastThrownGrenadeService;
    private readonly MessagingService _messagingService;
    private readonly PluginConfigService _pluginConfigService;

    /// <summary>
    /// The nade witch is currently being edited or the last saved nade
    /// </summary>
    private ConcurrentDictionary<CCSPlayerController, GrenadeJsonModel> SelectedGrenade { get; } = new();

    public GrenadeMenuService(
        CommandService commandService,
        PluginConfigService pluginConfigService,
        MessagingService messagingService,
        GrenadeStorageService grenadeStorageService,
        LastThrownGrenadeService lastThrownGrenadeService)
    {
        _commandService = commandService;
        _pluginConfigService = pluginConfigService;
        _messagingService = messagingService;
        _grenadeStorageService = grenadeStorageService;
        _lastThrownGrenadeService = lastThrownGrenadeService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnMapStart>(ListenersHandlerOnMapStart);

        _commandService.RegisterCommand(ChatCommands.GrenadeMenu,
            CommandActionOpenNadeMenu,
            ArgOption.NoArgs("Open global grenade menu"),
            Permissions.Flags.ReadGrenades);

        _commandService.RegisterCommand(ChatCommands.SelectGrenade,
            CommandActionSelectNade,
            [
                ArgOption.UInt("Select grenade to edit/throw by id", "grenade id"),
                ArgOption.String("Select grenade to edit/throw by name", "grenade name")
            ],
            [Permissions.Flags.WriteGrenades]);

        _commandService.RegisterCommand(ChatCommands.Throw,
            CommandActionThrow,
            [
                ArgOption.NoArgs("Throw the selected grenade"),
                ArgOption.UInt("Throw and select grenade by id", "grenade id"),
                ArgOption.String("Throw and select grenade by name", "grenade name")
            ],
            [Permissions.Flags.ReadGrenades]);

        _commandService.RegisterCommand(ChatCommands.SaveGrenade,
            CommandActionSave,
            ArgOption.String("Saves the last thrown grenade", "name"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.DeleteGrenade,
            CommandActionDelete,
            ArgOption.String("Delete saved grenade", "name"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.FindGrenade,
            CommandActionFind,
            ArgOption.String("Find grenade with specific name", "name"),
            Permissions.Flags.ReadGrenades);

        _commandService.RegisterCommand(ChatCommands.RenameGrenade,
            CommandHandlerRename,
            ArgOption.String("Rename selected grenade", "new name"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.GrenadeDescription,
            CommandHandlerDescription,
            ArgOption.String("Change the description of the selected grenade", "new description"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.ShowGrenadeTags,
            CommandHandlerShowGlobalGrenadeTags,
            ArgOption.NoArgs("Show all global grenade tags"),
            Permissions.Flags.ReadGrenades);

        _commandService.RegisterCommand(ChatCommands.AddTagToGrenade,
            CommandHandlerAddTag,
            ArgOption.String("Add tag to selected grenade", "tag"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.RemoveGrenadeTag,
            CommandHandlerRemoveTag,
            ArgOption.String("Remove tag from selected grenade", "tag"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.ClearGrenadeTags,
            CommandHandlerClearTags,
            ArgOption.NoArgs("Remove all tags from selected grenade"),
            Permissions.Flags.WriteGrenades);

        _commandService.RegisterCommand(ChatCommands.DeleteGrenadeTag,
            CommandHandlerDeleteTag,
            ArgOption.String("Remove tag from every grenade", "tag"),
            Permissions.Flags.WriteGrenades);

        base.Load(plugin);
    }

    private void ListenersHandlerOnMapStart(string _)
    {
        SelectedGrenade.Clear();
    }

    private CenterHtmlMenu GenerateGrenadeMenu(string name, List<GrenadeJsonModel> grenades)
    {
        var htmlMenu = new CenterHtmlMenu(name)
        {
            PostSelectAction = PostSelectAction.Nothing
        };

        htmlMenu.AddMenuOption("Lowest thrown grenade", (player, _) =>
        {
            var lastThrownGrenadeForPlayer = _lastThrownGrenadeService.GetLastThrownGrenade(player);
            if (lastThrownGrenadeForPlayer.IsError)
            {
                _messagingService.MsgToPlayerChat(player,
                    $"Failed to get your last thrown grenade. {lastThrownGrenadeForPlayer.ErrorMessage()}");
            }
            else
            {
                lastThrownGrenadeForPlayer.Value.TeleportToThrowPosition(player);
            }
        });

        foreach (var entry in grenades)
        {
            htmlMenu.AddMenuOption(entry.Name, (player, option) =>
            {
                var restoreSnapshot = RestoreSnapshot(player, entry.Name);
                if (restoreSnapshot.IsError)
                {
                    _messagingService.MsgToPlayerChat(player,
                        $"Teleport to grenade position failed. {restoreSnapshot.ErrorMessage()}");
                    return;
                }

                var selectNade = SelectNade(player, entry);
                if (selectNade.IsError)
                {
                    _messagingService.MsgToPlayerChat(player, selectNade.ErrorMessage());
                }

                option.Text = entry.Name;
            });
        }

        return htmlMenu;
    }

    private ErrorOr<Success> RestoreSnapshot(CCSPlayerController player, string grenadeName)
    {
        var getResult = _grenadeStorageService.Get(grenadeName, Server.MapName);
        if (getResult.IsError)
        {
            return getResult.FirstError;
        }

        var grenade = getResult.Value;
        if (player.IsValid == false || player.PlayerPawn.Value is null)
        {
            return Errors.PlayerNullOrNotValid();
        }

        grenade.TeleportToThrowPosition(player);
        _messagingService.MsgToPlayerChat(player, $"Teleported to grenade \"{grenade.Name}\" position");
        return Result.Success;
    }

    private ErrorOr<Success> SelectNade(CCSPlayerController player, GrenadeJsonModel grenade)
    {
        if (_pluginConfigService.Config.EnablePermissions &&
            AdminManager.PlayerHasPermissions(player, Permissions.Flags.WriteGrenades) == false)
        {
            return Errors.Fail("You dont have permissions to edit grenades");
        }

        SelectedGrenade[player] = grenade;
        return Result.Success;
    }

    private ErrorOr<GrenadeJsonModel> GetSelectedNade(CCSPlayerController player)
    {
        if (SelectedGrenade.TryGetValue(player, out var grenade) == false)
        {
            return Errors.Fail("No grenade selected");
        }

        if (AdminManager.PlayerHasPermissions(player, Permissions.Flags.WriteGrenades) == false)
        {
            return Errors.Fail("You dont have permissions to edit global grenades");
        }

        return grenade;
    }
}