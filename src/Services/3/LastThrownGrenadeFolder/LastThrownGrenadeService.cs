using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Extensions;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.SharedModels;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;

public class LastThrownGrenadeService : Base
{
    private readonly CommandService _commandService;
    private readonly ConcurrentDictionary<uint, Grenade> _entityIdToGrenade = new();

    private readonly ConcurrentDictionary<CCSPlayerController, GrenadeHistory> _lastThrownGrenade = new();
    private readonly ILogger<LastThrownGrenadeService> _logger;


    public LastThrownGrenadeService(ILogger<LastThrownGrenadeService> logger, CommandService commandService)
    {
        _logger = logger;
        _commandService = commandService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnEntitySpawned>(ListenerHandlerOnEntitySpawned);
        plugin.RegisterListener<Listeners.OnMapStart>(ListenerHandlerOnMapStart);
        plugin.RegisterListener<Listeners.OnEntityDeleted>(ListenerHandlerEntityDeletedMolotovDetonated);
        plugin.RegisterEventHandler<EventSmokegrenadeDetonate>(EventHandlerOnSmokeDetonate);
        plugin.RegisterEventHandler<EventFlashbangDetonate>(EventHandlerFlashbangDetonate);
        plugin.RegisterEventHandler<EventHegrenadeDetonate>(EventHandlerOnHeGrenadeDetonate);

        _commandService.RegisterCommand(ChatCommands.Rethrow,
            CommandHandlerRethrow,
            ArgOption.NoArgs("Rethrows the last thrown grenade"),
            Permissions.Flags.Rethrow);

        _commandService.RegisterCommand(ChatCommands.Last,
            CommandHandlerLast,
            ArgOption.NoArgs("Teleports to the last thrown grenade position"),
            Permissions.Flags.Last);

        _commandService.RegisterCommand(ChatCommands.Forward,
            CommandHandlerForward,
            new[]
            {
                ArgOption.NoArgs("Teleports to next grenade in the list of thrown grenade"),
                ArgOption.UInt(
                    "Moves [amount] position forward in the list of thrown grenades and teleports to the grenade at that spot",
                    "amount")
            },
            new[] { Permissions.Flags.Forward, Permissions.Flags.Back });

        _commandService.RegisterCommand(ChatCommands.Back,
            CommandHandlerBack,
            new[]
            {
                ArgOption.NoArgs("Teleports to the previous grenade in the list of thrown grenade"),
                ArgOption.UInt(
                    "Moves [amount] position backward in the list of thrown grenades and teleports to the grenade at that spot",
                    "amount")
            },
            new[] { Permissions.Flags.Forward, Permissions.Flags.Back });
        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _lastThrownGrenade.Clear();
        base.Unload(plugin);
    }

    public ErrorOr<Grenade> GetLastThrownGrenade(CCSPlayerController player)
    {
        if (_lastThrownGrenade.TryGetValue(player, out var grenadeHistory) == false)
        {
            return Errors.Fail("No grenades thrown yet");
        }

        return grenadeHistory.GetLatestSnapshotInHistory();
    }

    private void ListenerHandlerOnMapStart(string _)
    {
        _lastThrownGrenade.Clear();
    }

    private void ListenerHandlerOnEntitySpawned(CEntityInstance entity)
    {
        if (entity.Entity is null || entity.Entity.DesignerName.EndsWith("_projectile") == false)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            var projectile = new CBaseCSGrenadeProjectile(entity.Handle);
            if (projectile.Thrower.Value?.Controller.Value is null)
            {
                _logger.LogError("Failed to get projectile thrower");
                return;
            }

            var player = new CCSPlayerController(projectile.Thrower.Value.Controller.Value.Handle);
            if (player.PlayerPawn.Value is null)
            {
                _logger.LogError("Failed to get pawn of thrower");
                return;
            }

            var typeResult = projectile.GetGrenadeType();
            if (typeResult.IsError)
            {
                _logger.LogError("Failed to get grenade type. {Error}", typeResult.ErrorMessage());
                return;
            }

            // projectile.Globalname can be null.
            // Wrong annotation from CounterStrikeSharp
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // Return if its a artificially thrown grenade
            if (projectile.Globalname is not null && projectile.Globalname.Equals("custom"))
            {
                return;
            }

            var playerPosition = Position.CopyFrom(player.PlayerPawn.Value);
            var snapshot = new Grenade
            {
                Type = typeResult.Value,
                ThrowPosition = playerPosition.Pos,
                InitialPosition = projectile.InitialPosition,
                Angle = playerPosition.Angle,
                Velocity = projectile.InitialVelocity
            };

            // Add to last thrown grenades of player, if its a player thrown grenade
            if (_lastThrownGrenade.TryGetValue(player, out var grenadeHistory))
            {
                grenadeHistory.AddNewEntry(snapshot);
            }
            else
            {
                if (_lastThrownGrenade.TryAdd(player, new GrenadeHistory(snapshot)) == false)
                {
                    _logger.LogError(
                        "Failed to add last thrown grenade to history of player \"{PlayerName}\". This should never happen",
                        player.PlayerName);
                }
            }

            _entityIdToGrenade[entity.Index] = snapshot;
        });
    }

    #region CommandHandlers

    private ErrorOr<Success> CommandHandlerBack(CCSPlayerController player, CommandInfo commandInfo)
    {
        uint goBackByAmount = 1;
        if (commandInfo.GotArgsCount(1))
        {
            var getArgIntResult = commandInfo.GetArgUInt();
            if (getArgIntResult.IsError)
            {
                return Errors.Fail("Failed to parse arg to int");
            }

            goBackByAmount = getArgIntResult.Value;
        }

        if (_lastThrownGrenade.TryGetValue(player, out var grenadeHistory) == false)
        {
            return Errors.Fail("No grenade thrown yet");
        }


        var snapshot = grenadeHistory.Back(goBackByAmount);
        if (snapshot.IsError)
        {
            grenadeHistory.GetFirstSnapshotInHistory().TeleportToThrowPosition(player);
            return snapshot.FirstError;
        }

        snapshot.Value.TeleportToThrowPosition(player);
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerForward(CCSPlayerController player, CommandInfo commandInfo)
    {
        uint goForwardByAmount = 1;
        if (commandInfo.GotArgsCount(1))
        {
            var getArgIntResult = commandInfo.GetArgUInt();
            if (getArgIntResult.IsError)
            {
                return Errors.Fail("Failed to parse arg to int");
            }

            goForwardByAmount = getArgIntResult.Value;
        }

        if (_lastThrownGrenade.TryGetValue(player, out var grenadeHistory) == false)
        {
            return Errors.Fail("No grenade thrown yet");
        }

        var snapshot = grenadeHistory.Forward(goForwardByAmount);
        if (snapshot.IsError)
        {
            grenadeHistory.GetLatestSnapshotInHistory().TeleportToThrowPosition(player);
            return snapshot.FirstError;
        }

        snapshot.Value.TeleportToThrowPosition(player);
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerRethrow(CCSPlayerController player, CommandInfo commandInfo)
    {
        var lastThrownGrenadeResult = GetLastThrownGrenade(player);
        if (lastThrownGrenadeResult.IsError)
        {
            return lastThrownGrenadeResult.FirstError;
        }


        var throwGrenadeProjectileResult = lastThrownGrenadeResult.Value.ThrowGrenade(player);
        if (throwGrenadeProjectileResult.IsError)
        {
            _logger.LogError("Failed to throw grenade for player \"{PlayerName}\". {Error}", player.PlayerName,
                throwGrenadeProjectileResult.ErrorMessage());
            return Errors.Fail($"Failed to throw grenade. {throwGrenadeProjectileResult.ErrorMessage()}");
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerLast(CCSPlayerController player, CommandInfo commandInfo)
    {
        var lastThrownGrenadeResult = GetLastThrownGrenade(player);
        if (lastThrownGrenadeResult.IsError)
        {
            return lastThrownGrenadeResult.FirstError;
        }

        lastThrownGrenadeResult.Value.TeleportToThrowPosition(player);
        return Result.Success;
    }

    #endregion


    #region OnGrenadeDetonated

    private void ListenerHandlerEntityDeletedMolotovDetonated(CEntityInstance entity)
    {
        if (entity.DesignerName.Equals(DesignerNames.ProjectileMolotov) == false)
        {
            return;
        }

        var projectile = new CBaseCSGrenadeProjectile(entity.Handle);
        if (projectile.DetonationRecorded == false)
        {
            return;
        }

        if (projectile.Thrower.Value?.Controller.Value is null)
        {
            _logger.LogError("Failed to get thrower for grenade {DesignerName}", projectile.DesignerName);
            return;
        }

        var detonationPosition = projectile.AbsOrigin;
        if (detonationPosition is null)
        {
            _logger.LogError("Failed to get position of exploded molotov {EntityId}", entity.Index);
            return;
        }

        HandleOnDetonate(entity.Index, detonationPosition.X, detonationPosition.Y, detonationPosition.Z);
    }

    private HookResult EventHandlerOnHeGrenadeDetonate(EventHegrenadeDetonate @event, GameEventInfo info)
    {
        HandleOnDetonate((uint)@event.Entityid, @event.X, @event.Y, @event.Z);
        return HookResult.Continue;
    }

    private HookResult EventHandlerFlashbangDetonate(EventFlashbangDetonate @event, GameEventInfo info)
    {
        HandleOnDetonate((uint)@event.Entityid, @event.X, @event.Y, @event.Z);
        return HookResult.Continue;
    }

    private HookResult EventHandlerOnSmokeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        HandleOnDetonate((uint)@event.Entityid, @event.X, @event.Y, @event.Z);
        return HookResult.Continue;
    }

    private void HandleOnDetonate(uint entityIndex, float x, float y, float z)
    {
        if (_entityIdToGrenade.TryGetValue(entityIndex, out var grenade) == false)
        {
            return;
        }

        grenade.DetonationPosition = new Vector(x, y, z);
    }

    #endregion
}