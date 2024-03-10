using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services.Last.GameConfigFolder;

public class GameConfigService : Base
{
    private readonly ILogger<GameConfigService> _logger;

    private string _gameConfigName = null!;

    public GameConfigService(ILogger<GameConfigService> logger)
    {
        _logger = logger;
    }

    public override void Load(BasePlugin plugin)
    {
        _gameConfigName = $"{plugin.ModuleName}.cfg";
        CopyDefaultCopyIfNotExist(plugin.ModuleDirectory);
        plugin.RegisterListener<Listeners.OnMapStart>(ListenerHandlerOnMapStart);
        plugin.RegisterEventHandler<EventRoundAnnounceWarmup>(EventHandlerOnPlayerConnectFull);
        ExecuteDefaultConfig();

        base.Load(plugin);
    }

    private void CopyDefaultCopyIfNotExist(string moduleDirectory)
    {
        var configDestPath = Path.Combine(Server.GameDirectory, "csgo", "cfg", _gameConfigName);
        if (File.Exists(configDestPath) == false)
        {
            var configSrcPath = Path.Combine(moduleDirectory, _gameConfigName);
            var configFileLines = File.ReadAllLines(configSrcPath).Skip(6);
            File.WriteAllLines(configDestPath, configFileLines);
            _logger.LogInformation("Default \"{ConfigFileName}\" created", _gameConfigName);
        }
    }

    private HookResult EventHandlerOnPlayerConnectFull(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        ExecuteDefaultConfig();
        return HookResult.Continue;
    }

    private void ListenerHandlerOnMapStart(string _)
    {
        ExecuteDefaultConfig();
    }

    private void ExecuteDefaultConfig()
    {
        Server.ExecuteCommand($"exec {_gameConfigName}");
    }
}