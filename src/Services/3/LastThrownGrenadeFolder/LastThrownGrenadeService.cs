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
    private readonly ILogger<LastThrownGrenadeService> _logger;
    private readonly CommandService _commandService;

    private readonly ConcurrentDictionary<uint, Grenade> _entityIdToGrenade = new();
    private readonly ConcurrentDictionary<CCSPlayerController, GrenadeHistory> _lastThrownGrenade = new();


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
            [
                ArgOption.NoArgs("Teleports to next grenade in the list of thrown grenade"),
                ArgOption.UInt(
                    "Moves [amount] position forward in the list of thrown grenades and teleports to the grenade at that spot",
                    "amount")
            ],
            [Permissions.Flags.Forward, Permissions.Flags.Back]);

        _commandService.RegisterCommand(ChatCommands.Back,
            CommandHandlerBack,
            [
                ArgOption.NoArgs("Teleports to the previous grenade in the list of thrown grenade"),
                ArgOption.UInt(
                    "Moves [amount] position backward in the list of thrown grenades and teleports to the grenade at that spot",
                    "amount")
            ],
            [Permissions.Flags.Forward, Permissions.Flags.Back]);
        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _lastThrownGrenade.Clear();
        base.Unload(plugin);
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
        _entityIdToGrenade.Clear();
    }

    public void AddToLastThrownGrenades(CCSPlayerController player, Grenade grenade)
    {
        if (_lastThrownGrenade.TryGetValue(player, out var grenadeHistory))
        {
            grenadeHistory.AddNewEntry(grenade);
        }
        else
        {
            _lastThrownGrenade[player] = new GrenadeHistory(grenade);
        }

        _logger.LogInformation("{Player}({SteamId}) New grenade added to last thrown grenades", player.PlayerName,
            player.SteamID);
    }

    private void ListenerHandlerOnEntitySpawned(CEntityInstance entity)
    {
        if (entity.Entity is null || entity.Entity.DesignerName.EndsWith("_projectile") == false)
        {
            return;
        }

        Server.NextFrame(() =>
        {
            var grenade = new CBaseCSGrenadeProjectile(entity.Handle);
            if (grenade.Thrower.Value?.Controller.Value is null)
            {
                _logger.LogError("Failed to get projectile thrower");
                return;
            }

            var player = new CCSPlayerController(grenade.Thrower.Value.Controller.Value.Handle);
            if (player.PlayerPawn.Value is null)
            {
                _logger.LogError("Failed to get pawn of thrower");
                return;
            }

            var typeResult = grenade.GetGrenadeType();
            if (typeResult.IsError)
            {
                _logger.LogError("Failed to get grenade type. {Error}", typeResult.ErrorMessage());
                return;
            }

            // projectile.Globalname can be null.
            // Wrong annotation from CounterStrikeSharp
            // Return if its artificially thrown grenade
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (grenade.Globalname is not null && grenade.Globalname.Equals("custom"))
            {
                return;
            }

            // Copy the current position/velocity, so it does not change after the snapshot is created
            var playerPosition = Position.CopyFrom(player.PlayerPawn.Value);
            var initialPosition = new Vector(grenade.InitialPosition.X, grenade.InitialPosition.Y,
                grenade.InitialPosition.Z);
            var initialVelocity = new Vector(grenade.InitialVelocity.X, grenade.InitialVelocity.Y,
                grenade.InitialVelocity.Z);

            var snapshot = new Grenade
            {
                Type = typeResult.Value,
                ThrowPosition = playerPosition.Pos,
                InitialPosition = initialPosition,
                Angle = playerPosition.Angle,
                Velocity = initialVelocity
            };

            AddToLastThrownGrenades(player, snapshot);
            _entityIdToGrenade[entity.Index] = snapshot;
        });
    }


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
        if (_entityIdToGrenade.TryRemove(entityIndex, out var grenade) == false)
        {
            return;
        }

        grenade.DetonationPosition = new Vector(x, y, z);
    }

    #endregion
}