using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using Cs2PracticeMode.Services.Second.SettingsStorageFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services.Last.SettingsFolder;

public class SettingsMenuService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;
    private readonly SettingsStorageService _settingsStorageService;

    public SettingsMenuService(
        CommandService commandService,
        SettingsStorageService settingsStorageService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _settingsStorageService = settingsStorageService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        _commandService.RegisterCommand(ChatCommands.Settings,
            CommandActionOpenSettingsMenu,
            ArgOption.NoArgs("Opens the settings menu"),
            Permissions.Flags.Settings);
        base.Load(plugin);
    }

    private ErrorOr<Success> CommandActionOpenSettingsMenu(CCSPlayerController player, CommandInfo commandInfo)
    {
        var openSettingsMenuResult = OpenSettingsMenu(player);
        if (openSettingsMenuResult.IsError)
        {
            return openSettingsMenuResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> OpenSettingsMenu(CCSPlayerController player)
    {
        var htmlMenu = new CenterHtmlMenu("Settings")
        {
            PostSelectAction = PostSelectAction.Nothing
        };

        htmlMenu.AddMenuOption("Print current settings", (selectPlayer, _) =>
        {
            var settings = _settingsStorageService.Get();
            _messagingService.MsgToPlayerChat(selectPlayer,
                $"Smoke colors: {EnabledOrDisabledString(settings.DisableSmokeColors)}");
            _messagingService.MsgToPlayerChat(selectPlayer,
                $"Blind time print: {EnabledOrDisabledString(settings.DisableBlindTimePrint)}");
            _messagingService.MsgToPlayerChat(selectPlayer,
                $"Damage pint: {EnabledOrDisabledString(settings.DisableDamagePrint)}");
        });

        htmlMenu.AddMenuOption("Enable/Disable smoke colors",
            (selectPlayer, _) =>
            {
                var settings = _settingsStorageService.Get();
                settings.DisableSmokeColors = !settings.DisableSmokeColors;

                var update = _settingsStorageService.Update(settings);
                _messagingService.MsgToPlayerChat(selectPlayer,
                    update.IsError
                        ? $"Failed to update settings. {update.ErrorMessage()}"
                        : $"Smoke colors are now {EnabledOrDisabledString(settings.DisableSmokeColors)}");
            });

        htmlMenu.AddMenuOption("Enable/Disable blind time print",
            (selectPlayer, _) =>
            {
                var settings = _settingsStorageService.Get();
                settings.DisableBlindTimePrint = !settings.DisableBlindTimePrint;

                var update = _settingsStorageService.Update(settings);
                _messagingService.MsgToPlayerChat(selectPlayer,
                    update.IsError
                        ? $"Failed to update settings. {update.ErrorMessage()}"
                        : $"Blind time prints are now {EnabledOrDisabledString(settings.DisableBlindTimePrint)}");
            });

        htmlMenu.AddMenuOption("Enable/Disable player damage pint",
            (selectPlayer, _) =>
            {
                var settings = _settingsStorageService.Get();
                settings.DisableDamagePrint = !settings.DisableDamagePrint;

                var update = _settingsStorageService.Update(settings);
                _messagingService.MsgToPlayerChat(selectPlayer,
                    update.IsError
                        ? $"Failed to update settings. {update.ErrorMessage()}"
                        : $"Player damage pints are now {EnabledOrDisabledString(settings.DisableDamagePrint)}");
            });

        var openHtmlMenu = _messagingService.OpenHtmlMenu(player, htmlMenu);
        if (openHtmlMenu.IsError)
        {
            return openHtmlMenu.FirstError;
        }

        return Result.Success;
    }

    private static string EnabledOrDisabledString(bool isEnabled)
    {
        return isEnabled ? "disabled" : "enabled";
    }
}