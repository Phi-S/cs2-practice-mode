using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using Cs2PracticeMode.Services._3.SettingsFolder;
using Cs2PracticeMode.Storage.Single;
using ErrorOr;

namespace Cs2PracticeMode.Services._1.SettingsStorageFolder;

public class SettingsStorageService : Base
{
    private readonly PluginConfigService _pluginConfigService;
    private Settings _settings = null!;

    private IStorageSingle<Settings> _storage = null!;

    public SettingsStorageService(PluginConfigService pluginConfigService) : base(LoadOrder.High)
    {
        _pluginConfigService = pluginConfigService;
    }

    public override void Load(BasePlugin plugin)
    {
        const string name = "settings";
        if (_pluginConfigService.Config.DataLocation.ToLower().StartsWith("local#"))
        {
            var dataLocation = _pluginConfigService.Config.DataLocation.Replace("local#", "");
            _storage = new LocalStorageSingle<Settings>(dataLocation, name);
        }
        else if (_pluginConfigService.Config.DataLocation.ToLower().StartsWith("postgres#"))
        {
            var dataLocation = _pluginConfigService.Config.DataLocation.Replace("postgres#", "");
            _storage = new PostgresStorageSingle<Settings>(dataLocation, name);
        }
        else
        {
            throw new ApplicationException(
                $"Data location not implemented. \"{_pluginConfigService.Config.DataLocation}\"");
        }

        if (_storage.Exists() == false)
        {
            var addResult = _storage.AddOrUpdate(new Settings());
            if (addResult.IsError)
            {
                throw new ApplicationException($"Failed to create new settings. {addResult.ErrorMessage()}");
            }

            var getResult = _storage.Get();
            if (getResult.IsError)
            {
                throw new ApplicationException($"Failed to get newly created settings. {addResult.ErrorMessage()}");
            }

            _settings = getResult.Value;
        }
        else
        {
            var getResult = _storage.Get();
            if (getResult.IsError)
            {
                throw new ApplicationException($"Failed to get settings. {getResult.ErrorMessage()}");
            }

            _settings = getResult.Value;
        }

        base.Load(plugin);
    }

    public Settings Get()
    {
        return (Settings)_settings.Clone();
    }

    public ErrorOr<Success> Update(Settings settings)
    {
        var update = _storage.AddOrUpdate(settings);

        if (update.IsError)
        {
            return update.FirstError;
        }

        _settings = settings;
        return update.Value;
    }
}