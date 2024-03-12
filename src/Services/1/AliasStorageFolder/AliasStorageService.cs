using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using Cs2PracticeMode.Storage.Collection;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services._1.AliasStorageFolder;

public class AliasStorageService : Base
{
    private readonly ILogger<AliasStorageService> _logger;
    private readonly PluginConfigService _pluginConfigService;

    private IStorageCollection<GlobalAliasJsonModel> _globalAliasStorageCollection = null!;
    private IStorageCollection<PlayerAliasJsonModel> _playerAliasStorageCollection = null!;

    private readonly object _aliasLock = new();
    private List<GlobalAliasJsonModel> GlobalAliasesCache { get; } = new();
    private List<PlayerAliasJsonModel> PlayerAliasesCache { get; } = new();

    public AliasStorageService(ILogger<AliasStorageService> logger, PluginConfigService pluginConfigService) : base(
        LoadOrder.High)
    {
        _logger = logger;
        _pluginConfigService = pluginConfigService;
    }

    public override void Load(BasePlugin plugin)
    {
        if (_pluginConfigService.Config.DataLocation.ToLower().StartsWith("local#"))
        {
            var dataLocation = _pluginConfigService.Config.DataLocation.Replace("local#", "");
            var localFilePath = Path.Combine(dataLocation, "grenades");
            _globalAliasStorageCollection = new LocalStorageCollection<GlobalAliasJsonModel>(localFilePath);
            _playerAliasStorageCollection = new LocalStorageCollection<PlayerAliasJsonModel>(localFilePath);
            _logger.LogInformation("Alias storage is using local storage");
        }
        else if (_pluginConfigService.Config.DataLocation.ToLower().StartsWith("postgres#"))
        {
            var dataLocation = _pluginConfigService.Config.DataLocation.Replace("postgres#", "");
            _globalAliasStorageCollection =
                new PostgresStorageCollection<GlobalAliasJsonModel>(dataLocation, "grenades");
            _playerAliasStorageCollection =
                new PostgresStorageCollection<PlayerAliasJsonModel>(dataLocation, "grenades");
            _logger.LogInformation("Alias storage is using postgres storage");
        }
        else
        {
            throw new NotSupportedException(
                $"Data location not implemented. \"{_pluginConfigService.Config.DataLocation}\"");
        }

        var updateGlobalAliasesCache = UpdateGlobalAliasesCache();
        if (updateGlobalAliasesCache.IsError)
        {
            throw new Exception($"Failed to cache global aliases. \"{updateGlobalAliasesCache.ErrorMessage()}\"");
        }

        var updatePlayerAliasesCache = UpdatePlayerAliasesCache();
        if (updatePlayerAliasesCache.IsError)
        {
            throw new Exception($"Failed to cache player aliases. \"{updatePlayerAliasesCache.ErrorMessage()}\"");
        }

        base.Load(plugin);
    }

    private ErrorOr<Success> UpdateGlobalAliasesCache()
    {
        lock (_aliasLock)
        {
            var aliases = _globalAliasStorageCollection.GetAll();
            if (aliases.IsError)
            {
                return aliases.FirstError;
            }

            GlobalAliasesCache.Clear();
            GlobalAliasesCache.AddRange(aliases.Value);
        }

        return Result.Success;
    }

    private ErrorOr<Success> UpdatePlayerAliasesCache()
    {
        lock (_aliasLock)
        {
            var aliases = _playerAliasStorageCollection.GetAll();
            if (aliases.IsError)
            {
                return aliases.FirstError;
            }

            PlayerAliasesCache.Clear();
            PlayerAliasesCache.AddRange(aliases.Value);
        }

        return Result.Success;
    }

    private bool GlobalAliasExists(string alias)
    {
        lock (_aliasLock)
        {
            alias = alias.ToLower().Trim();
            return GlobalAliasesCache.Exists(a => a.Alias.ToLower().Trim().Equals(alias));
        }
    }

    private bool PlayerAliasExists(CBasePlayerController player, string alias)
    {
        lock (_aliasLock)
        {
            alias = alias.ToLower().Trim();
            return PlayerAliasesCache.Exists(
                a => a.PlayerSteamId == player.SteamID && a.Alias.ToLower().Trim() == alias);
        }
    }

    public ErrorOr<string> GetCommandForGlobalAlias(string alias)
    {
        lock (_aliasLock)
        {
            alias = alias.ToLower().Trim();
            var foundCommand = GlobalAliasesCache.FirstOrDefault(a => a.Alias.ToLower().Trim() == alias);
            if (foundCommand is null)
            {
                return Errors.Fail($"No command found for alias \"{alias}\"");
            }

            return foundCommand.Command;
        }
    }

    public ErrorOr<string> GetCommandForPlayerAlias(CCSPlayerController player, string alias)
    {
        lock (_aliasLock)
        {
            alias = alias.ToLower().Trim();
            var foundCommand = PlayerAliasesCache.FirstOrDefault(a =>
                a.PlayerSteamId == player.SteamID && a.Alias.ToLower().Trim() == alias);
            if (foundCommand is null)
            {
                return Errors.Fail($"No command found for alias \"{alias}\"");
            }

            return foundCommand.Command;
        }
    }

    public ErrorOr<Success> AddGlobalAlias(string alias, string command)
    {
        lock (_aliasLock)
        {
            if (GlobalAliasExists(alias))
            {
                return Errors.Fail($"Global alias \"{alias}\" already exists");
            }

            var newAlias = new GlobalAliasJsonModel
            {
                Alias = alias,
                Command = command,
                UpdatedUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow
            };
            var add = _globalAliasStorageCollection.Add(newAlias);
            if (add.IsError)
            {
                return add.FirstError;
            }

            var updateGlobalAliasesCache = UpdateGlobalAliasesCache();
            if (updateGlobalAliasesCache.IsError)
            {
                return updateGlobalAliasesCache.FirstError;
            }

            return Result.Success;
        }
    }

    public ErrorOr<Success> AddPlayerAlias(CCSPlayerController player, string alias, string command)
    {
        lock (_aliasLock)
        {
            if (GlobalAliasExists(alias))
            {
                return Errors.Fail($"Global alias \"{alias}\" already exists");
            }
            
            if (PlayerAliasExists(player, alias))
            {
                return Errors.Fail($"Player alias \"{alias}\" already exists for player with steamid {player.SteamID}");
            }

            var newAlias = new PlayerAliasJsonModel
            {
                PlayerSteamId = player.SteamID,
                Alias = alias,
                Command = command,
                UpdatedUtc = DateTime.UtcNow,
                CreatedUtc = DateTime.UtcNow
            };
            var add = _playerAliasStorageCollection.Add(newAlias);
            if (add.IsError)
            {
                return add.FirstError;
            }

            var updatePlayerAliasesCache = UpdatePlayerAliasesCache();
            if (updatePlayerAliasesCache.IsError)
            {
                return updatePlayerAliasesCache.FirstError;
            }

            return Result.Success;
        }
    }

    public ErrorOr<Deleted> DeleteGlobalAlias(string alias)
    {
        lock (_aliasLock)
        {
            alias = alias.ToLower().Trim();
            var globalAliasJsonModel = GlobalAliasesCache.FirstOrDefault(a => a.Alias.ToLower().Trim() == alias);
            if (globalAliasJsonModel is null)
            {
                return Errors.Fail($"Global alias \"{alias}\" not registered");
            }

            var delete = _globalAliasStorageCollection.Delete(globalAliasJsonModel.Id);
            if (delete.IsError)
            {
                return delete.FirstError;
            }

            var updateGlobalAliasesCache = UpdateGlobalAliasesCache();
            if (updateGlobalAliasesCache.IsError)
            {
                return updateGlobalAliasesCache.FirstError;
            }
        }

        return Result.Deleted;
    }

    public ErrorOr<Deleted> DeletePlayerAlias(CCSPlayerController player, string alias)
    {
        lock (_aliasLock)
        {
            alias = alias.ToLower().Trim();
            var globalAliasJsonModel = PlayerAliasesCache.FirstOrDefault(a =>
                a.PlayerSteamId == player.SteamID && a.Alias.ToLower().Trim() == alias);
            if (globalAliasJsonModel is null)
            {
                return Errors.Fail(
                    $"Player alias \"{alias}\" not registered for player with the steamid \"{player.SteamID}\"");
            }

            var delete = _globalAliasStorageCollection.Delete(globalAliasJsonModel.Id);
            if (delete.IsError)
            {
                return delete.FirstError;
            }

            var updatePlayerAliasesCache = UpdatePlayerAliasesCache();
            if (updatePlayerAliasesCache.IsError)
            {
                return updatePlayerAliasesCache.FirstError;
            }
        }

        return Result.Deleted;
    }
}