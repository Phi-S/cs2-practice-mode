using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Config;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services._0.PluginConfigFolder;

public class PluginConfigService : Base
{
    private readonly ILogger<PluginConfigService> _logger;

    public PluginConfigService(ILogger<PluginConfigService> logger) : base(LoadOrder.Highest)
    {
        _logger = logger;
    }

    public Cs2PracModeConfig Config { get; private set; } = new();

    public override void Load(BasePlugin plugin)
    {
        var pluginName = Path.GetFileName(plugin.ModuleDirectory);
        var config = ConfigManager.Load<Cs2PracModeConfig>(pluginName);
        if (string.IsNullOrWhiteSpace(config.DataLocation))
        {
            var localDataLocation = Path.Combine(plugin.ModuleDirectory, "storage");
            if (Directory.Exists(localDataLocation) == false)
            {
                Directory.CreateDirectory(localDataLocation);
            }

            config.DataLocation = $"local#{localDataLocation}";
            _logger.LogInformation(
                "No data location provided in plugin config. Using default data location: \"{DataLocation}\"",
                config.DataLocation);
        }

        Config = config;
        base.Load(plugin);
    }
}