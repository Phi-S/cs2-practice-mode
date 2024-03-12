using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using ErrorOr;

namespace Cs2PracticeMode.Services._2.MessagingFolder;

public class MessagingService : Base
{
    private readonly ConcurrentDictionary<CCSPlayerController, (CenterHtmlMenu centerHtmlMenu, bool canBeOverwriten)>
        _openHtmlMenus = new();

    private readonly ConcurrentDictionary<CCSPlayerController, (HtmlPrint htmlPrint, DateTime? hideUtc)>
        _openHtmlPrints = new();

    private readonly PluginConfigService _pluginConfigService;
    private BasePlugin _plugin = null!;

    public MessagingService(PluginConfigService pluginConfigService) : base(LoadOrder.AboveNormal)
    {
        _pluginConfigService = pluginConfigService;
    }

    public override void Load(BasePlugin plugin)
    {
        _plugin = plugin;
        plugin.RegisterListener<Listeners.OnTick>(OnTick);

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        foreach (var openHtmlMenu in _openHtmlMenus)
        {
            MenuManager.CloseActiveMenu(openHtmlMenu.Key);
        }

        _openHtmlMenus.Clear();
        _openHtmlPrints.Clear();

        base.Unload(plugin);
    }

    private bool PlayerHtmlBusy(CCSPlayerController player)
    {
        if (_openHtmlMenus.TryGetValue(player, out var currentHtmlMenu) && currentHtmlMenu.canBeOverwriten == false)
        {
            if (MenuManager.GetActiveMenu(player) is null)
            {
                _openHtmlMenus.TryRemove(player, out _);
            }
            else
            {
                return true;
            }
        }

        return _openHtmlPrints.ContainsKey(player);
    }

    public void MsgToPlayerChat(CCSPlayerController player, string message, bool usePrefix = true)
    {
        var prefix = usePrefix ? $"{_pluginConfigService.Config.ChatPrefix} " : "";
        Server.NextFrame(() => player.PrintToChat($"{prefix}{message}"));
    }

    public void MsgToPlayerCenter(CCSPlayerController player, string message)
    {
        Server.NextFrame(() => player.PrintToCenter(message));
    }

    public void MsgToAll(string message, bool usePrefix = true)
    {
        var prefix = usePrefix ? $"{_pluginConfigService.Config.ChatPrefix} " : "";
        Server.NextFrame(() => Server.PrintToChatAll($"{prefix}{message}"));
    }

    public ErrorOr<Success> OpenHtmlMenu(CCSPlayerController player, CenterHtmlMenu menu, bool canBeOverwritten = false)
    {
        if (PlayerHtmlBusy(player) &&
            MenuManager.GetActiveMenu(player) is not null)
        {
            return Errors.Fail("Another html menu is already open");
        }

        MenuManager.OpenCenterHtmlMenu(_plugin, player, menu);
        _openHtmlMenus[player] = (menu, canBeOverwritten);
        return Result.Success;
    }

    public ErrorOr<HtmlPrint> ShowCenterHtml(CCSPlayerController player, string html)
    {
        return ShowCenterHtml(player, html, null);
    }

    private ErrorOr<HtmlPrint> ShowCenterHtml(CCSPlayerController player, string html, DateTime? hideUtc)
    {
        if (PlayerHtmlBusy(player))
        {
            return Errors.Fail("Player is already displaying html content");
        }

        var newPrint = new HtmlPrint(html);
        _openHtmlPrints[player] = (newPrint, hideUtc);
        return newPrint;
    }

    public ErrorOr<Success> HideCenterHtml(CCSPlayerController player, HtmlPrint htmlPrint)
    {
        if (_openHtmlPrints.TryGetValue(player, out var currentPrint))
        {
            if (currentPrint.htmlPrint != htmlPrint)
            {
                Errors.Fail($"Given html print dose not match current html print for player \"{player.PlayerName}\"");
            }

            _openHtmlPrints.TryRemove(player, out _);
        }
        else
        {
            return Errors.Fail($"No html print for player \"{player.PlayerName}\" to hide");
        }

        return Result.Success;
    }

    private void OnTick()
    {
        foreach (var player in _openHtmlPrints.Keys)
        {
            if (player.IsValid == false ||
                _openHtmlPrints.TryGetValue(player, out var htmlPrintForPlayer) == false ||
                htmlPrintForPlayer.hideUtc >= DateTime.UtcNow)
            {
                _openHtmlPrints.TryRemove(player, out _);
                continue;
            }

            player.PrintToCenterHtml(htmlPrintForPlayer.htmlPrint.Content);
        }
    }
}