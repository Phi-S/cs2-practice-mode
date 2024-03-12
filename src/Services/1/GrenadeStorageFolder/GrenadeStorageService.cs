using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using Cs2PracticeMode.Storage.Collection;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services._1.GrenadeStorageFolder;

public class GrenadeStorageService : Base
{
    private readonly ILogger<GrenadeStorageService> _logger;
    private readonly PluginConfigService _pluginConfigService;

    private readonly object _grenadesLock = new();
    private List<GrenadeJsonModel> _grenades = null!;
    private IStorageCollection<GrenadeJsonModel> _storageCollection = null!;

    public GrenadeStorageService(ILogger<GrenadeStorageService> logger, PluginConfigService pluginConfigService) :
        base(LoadOrder.High)
    {
        _logger = logger;
        _pluginConfigService = pluginConfigService;
    }

    public override void Load(BasePlugin plugin)
    {
        const string name = "grenades";
        if (_pluginConfigService.Config.DataLocation.ToLower().StartsWith("local#"))
        {
            var dataLocation = _pluginConfigService.Config.DataLocation.Replace("local#", "");
            var localFilePath = Path.Combine(dataLocation, name);
            _storageCollection = new LocalStorageCollection<GrenadeJsonModel>(localFilePath);
        }
        else if (_pluginConfigService.Config.DataLocation.ToLower().StartsWith("postgres#"))
        {
            var dataLocation = _pluginConfigService.Config.DataLocation.Replace("postgres#", "");
            _storageCollection = new PostgresStorageCollection<GrenadeJsonModel>(dataLocation, name);
        }
        else
        {
            throw new NotSupportedException(
                $"Data location not implemented. \"{_pluginConfigService.Config.DataLocation}\"");
        }

        var updateGrenadesCache = UpdateGrenadesCache();
        if (updateGrenadesCache.IsError)
        {
            throw new Exception($"Failed to update grenade cache. \"{updateGrenadesCache.ErrorMessage()}\"");
        }

        base.Load(plugin);
    }

    private ErrorOr<Success> UpdateGrenadesCache()
    {
        lock (_grenadesLock)
        {
            var getAllResult = _storageCollection.GetAll();
            if (getAllResult.IsError)
            {
                return getAllResult.FirstError;
            }

            _grenades = getAllResult.Value;
            return Result.Success;
        }
    }

    public List<GrenadeJsonModel> GetWhere(Func<GrenadeJsonModel, bool> predicate)
    {
        lock (_grenadesLock)
        {
            return _grenades.Where(predicate).ToList();
        }
    }

    public ErrorOr<GrenadeJsonModel> Get(uint grenadeId)
    {
        var grenadesWithId = GetWhere(g => g.Id == grenadeId);
        if (grenadesWithId.Count == 0)
        {
            return Errors.Fail($"No Grenade found with id \"{grenadeId}\"");
        }
        
        if (grenadesWithId.Count > 1)
        {
            _logger.LogError("Multiple Grenades found with id \"{GrenadeId}\". This should never happen", grenadeId);
            return Errors.Fail($"Multiple Grenades found with id \"{grenadeId}\". This should never happen");
        }
        
        return grenadesWithId.First();
    }

    public ErrorOr<GrenadeJsonModel> Get(string grenadeName, string map)
    {
        var cleanedGrenadeName = grenadeName.ToLower().Trim();
        var cleanedMapName = map.ToLower().Trim();
        lock (_grenadesLock)
        {
            var foundGrenade = _grenades.FirstOrDefault(g =>
                g.Map.ToLower().Trim().Equals(cleanedMapName) &&
                g.Name.ToLower().Trim().Equals(cleanedGrenadeName));

            if (foundGrenade is null)
            {
                return Errors.Fail($"Grenade \"{grenadeName}\" not found");
            }

            return foundGrenade;
        }
    }

    public ErrorOr<Success> Add(GrenadeJsonModel grenade)
    {
        lock (_grenadesLock)
        {
            var addResult = _storageCollection.Add(grenade);
            if (addResult.IsError)
            {
                return addResult.FirstError;
            }

            var updateGrenadesCache = UpdateGrenadesCache();
            if (updateGrenadesCache.IsError)
            {
                return updateGrenadesCache.FirstError;
            }

            return Result.Success;
        }
    }

    public ErrorOr<Deleted> Delete(uint id)
    {
        lock (_grenadesLock)
        {
            var delete = _storageCollection.Delete(id);
            if (delete.IsError)
            {
                return delete.FirstError;
            }

            var updateGrenadesCache = UpdateGrenadesCache();
            if (updateGrenadesCache.IsError)
            {
                return updateGrenadesCache.FirstError;
            }

            return Result.Deleted;
        }
    }

    public ErrorOr<Success> Update(GrenadeJsonModel grenade)
    {
        lock (_grenadesLock)
        {
            var update = _storageCollection.Update(grenade);
            if (update.IsError)
            {
                return update.FirstError;
            }

            var updateGrenadesCache = UpdateGrenadesCache();
            if (updateGrenadesCache.IsError)
            {
                return updateGrenadesCache.FirstError;
            }

            return Result.Success;
        }
    }
}