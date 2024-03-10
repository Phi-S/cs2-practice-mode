using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using Cs2PracticeMode.SharedModels;
using ErrorOr;

namespace Cs2PracticeMode.Services.Last.SpawnFolder;

public class SpawnService : Base
{
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;

    private MapSpawns? _mapSpawns;

    public SpawnService(CommandService commandService,
        MessagingService messagingService)
    {
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnMapStart>(ListenerHandlerOnMapStart);
        _commandService.RegisterCommand(
            ChatCommands.Spawn,
            CommandHandlerSpawn,
            ArgOption.SingleInt("Teleport to spawn", "spawn number"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.TSpawn,
            CommandHandlerTSpawn,
            ArgOption.SingleInt("Teleport to t spawn", "t spawn number"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.CtSpawn,
            CommandHandlerCtSpawn,
            ArgOption.SingleInt("Teleport to ct spawn", "t spawn number"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.BestSpawn,
            CommandHandlerBestSpawn,
            ArgOption.NoArgs("Teleport to the best spawn based on your current position"));

        _commandService.RegisterCommand(ChatCommands.WorstSpawn,
            CommandHandlerWorstSpawn,
            ArgOption.NoArgs("Teleport to the worst spawn based on your current position"));

        base.Load(plugin);
    }

    private void ListenerHandlerOnMapStart(string _)
    {
        _mapSpawns = GetSpawnForCurrentMap();
    }

    private ErrorOr<int> GetSpawnNumberFromArgs(CommandInfo commandInfo)
    {
        if (commandInfo.ArgCount != 2)
        {
            return Errors.Fail("Wrong number of arguments");
        }

        var spawnNumberResult = commandInfo.GetArgInt();
        if (spawnNumberResult.IsError)
        {
            return spawnNumberResult.FirstError;
        }

        var spawnNumber = spawnNumberResult.Value;
        if (spawnNumber <= 0)
        {
            return Errors.Fail($"Spawn {spawnNumber} is not a valid spawn");
        }

        return spawnNumber;
    }

    private MapSpawns GetSpawnForCurrentMap()
    {
        var tSpawns = GetSpawns(CsTeam.Terrorist);
        var ctSpawn = GetSpawns(CsTeam.CounterTerrorist);
        return new MapSpawns(tSpawns, ctSpawn);
    }

    private List<Position> GetSpawns(CsTeam team)
    {
        string teamString;
        if (team == CsTeam.Terrorist)
        {
            teamString = "terrorist";
        }
        else if (team == CsTeam.CounterTerrorist)
        {
            teamString = "counterterrorist";
        }
        else
        {
            return new List<Position>();
        }

        var result = new List<Position>();
        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>($"info_player_{teamString}").ToList();
        var minPrio = 1;
        foreach (var spawn in spawns)
            if (spawn.IsValid && spawn.Enabled && spawn.Priority <= minPrio)
            {
                minPrio = spawn.Priority;
            }

        foreach (var spawn in spawns)
            if (spawn.IsValid && spawn.Enabled && spawn.Priority == minPrio)
            {
                result.Add(
                    new Position(
                        spawn.CBodyComponent!.SceneNode!.AbsOrigin,
                        spawn.CBodyComponent.SceneNode.AbsRotation)
                );
            }

        return result;
    }

    private ErrorOr<Success> TeleportToTeamSpawn(CCSPlayerController player, int spawnNumber, CsTeam team = CsTeam.None)
    {
        if (_mapSpawns is null)
        {
            return Errors.Fail("Failed to get spawn for current map");
        }

        if (spawnNumber <= 0)
        {
            return Errors.Fail($"Spawn number \"{spawnNumber}\" is not valid");
        }

        var targetTeam = team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist
            ? (CsTeam)player.TeamNum
            : team;

        List<Position> spawns;
        if (targetTeam == CsTeam.Terrorist)
        {
            spawns = _mapSpawns.TerroristSpawns;
        }
        else if (targetTeam == CsTeam.CounterTerrorist)
        {
            spawns = _mapSpawns.CounterTerroristSpawns;
        }
        else
        {
            return Errors.Fail($"Team \"{team.ToString()}\" is not valid to get spawn");
        }

        if (spawns.Count < spawnNumber)
        {
            return Errors.Fail($"No spawns found for team \"{team.ToString()}\" on this map");
        }

        var playerPawn = player.PlayerPawn;
        if (playerPawn.IsValid == false || playerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        var spawn = spawns[spawnNumber - 1];
        playerPawn.Value.Teleport(
            spawn.Pos,
            spawn.Angle,
            new Vector(0, 0, 0));
        _messagingService.MsgToPlayerChat(player, $"Teleported to spawn {spawnNumber}");
        return Result.Success;
    }


    #region CommandHandlers

    private ErrorOr<Success> CommandHandlerWorstSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        if (_mapSpawns is null)
        {
            return Errors.Fail("Failed to get spawn for current map");
        }

        List<Position> spawns;
        if (player.Team == CsTeam.Terrorist)
        {
            spawns = _mapSpawns.TerroristSpawns;
        }
        else if (player.Team == CsTeam.CounterTerrorist)
        {
            spawns = _mapSpawns.CounterTerroristSpawns;
        }
        else
        {
            return Errors.Fail($"Cant get spawn for team \"{player.Team.ToString()}\"");
        }

        var playerPosition = Position.CopyFrom(player.PlayerPawn.Value);
        (int spawnNumber, float distance)? worst = null;
        for (var i = 0; i < spawns.Count; i++)
        {
            var spawn = spawns[i];
            var distance = playerPosition.AbsolutDistance(spawn);

            if (worst is null || worst.Value.distance < distance)
            {
                worst = (i + 1, distance);
            }
        }

        if (worst is null)
        {
            return Errors.Fail("Failed to find worst spawn");
        }

        var teleportResult = TeleportToTeamSpawn(player, worst.Value.spawnNumber);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerBestSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        if (_mapSpawns is null)
        {
            return Errors.Fail("Failed to get spawn for current map");
        }

        List<Position> spawns;
        if (player.Team == CsTeam.Terrorist)
        {
            spawns = _mapSpawns.TerroristSpawns;
        }
        else if (player.Team == CsTeam.CounterTerrorist)
        {
            spawns = _mapSpawns.CounterTerroristSpawns;
        }
        else
        {
            return Errors.Fail($"Cant get spawn for team \"{player.Team.ToString()}\"");
        }

        var playerPosition = Position.CopyFrom(player.PlayerPawn.Value);
        (int spawnNumber, float distance)? best = null;
        for (var i = 0; i < spawns.Count; i++)
        {
            var spawn = spawns[i];
            var distance = playerPosition.AbsolutDistance(spawn);

            if (best is null || best.Value.distance > distance)
            {
                best = (i + 1, distance);
            }
        }

        if (best is null)
        {
            return Errors.Fail("Failed to find best spawn");
        }

        var teleportResult = TeleportToTeamSpawn(player, best.Value.spawnNumber);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerCtSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        var spawnNumber = GetSpawnNumberFromArgs(commandInfo);
        if (spawnNumber.IsError)
        {
            return spawnNumber.FirstError;
        }

        var teleportResult = TeleportToTeamSpawn(player, spawnNumber.Value, CsTeam.CounterTerrorist);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerTSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        var spawnNumber = GetSpawnNumberFromArgs(commandInfo);
        if (spawnNumber.IsError)
        {
            return spawnNumber.FirstError;
        }

        var teleportResult = TeleportToTeamSpawn(player, spawnNumber.Value, CsTeam.Terrorist);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        var spawnNumber = GetSpawnNumberFromArgs(commandInfo);
        if (spawnNumber.IsError)
        {
            return spawnNumber.FirstError;
        }

        var teleportResult = TeleportToTeamSpawn(player, spawnNumber.Value);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    #endregion
}