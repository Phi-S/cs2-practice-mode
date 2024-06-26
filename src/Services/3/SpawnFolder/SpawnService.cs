﻿using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._1.SettingsStorageFolder;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using Cs2PracticeMode.SharedModels;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services._3.SpawnFolder;

public class SpawnService : Base
{
    private readonly ILogger<SpawnService> _logger;
    private readonly CommandService _commandService;
    private readonly MessagingService _messagingService;
    private readonly SettingsStorageService _settingsStorageService;

    private MapSpawns? _mapSpawnsCache;
    private readonly object _mapSpawnCacheLock = new();

    private readonly List<int> _spawnMarkerLaserEntityIds = [];
    private bool _shouldMarkSpawns = true;

    public SpawnService(ILogger<SpawnService> logger, CommandService commandService, MessagingService messagingService,
        SettingsStorageService settingsStorageService)
    {
        _logger = logger;
        _commandService = commandService;
        _messagingService = messagingService;
        _settingsStorageService = settingsStorageService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventRoundStart>((_, _) =>
        {
            if (_shouldMarkSpawns)
            {
                MarkSpawns();
            }

            return HookResult.Continue;
        });

        _settingsStorageService.OnSettingsUpdated += settings =>
        {
            if (settings.DisableSpawnMarker == false)
            {
                if (_shouldMarkSpawns)
                {
                    return;
                }

                MarkSpawns();
                _shouldMarkSpawns = true;

                return;
            }

            if (_shouldMarkSpawns && _spawnMarkerLaserEntityIds.Count != 0)
            {
                foreach (var spawnMarkerLaserEntityId in _spawnMarkerLaserEntityIds)
                {
                    var entity = Utilities.GetEntityFromIndex<CBaseEntity>(spawnMarkerLaserEntityId);

                    if (entity?.DesignerName == "env_beam")
                    {
                        entity.Remove();
                    }
                }
            }

            _shouldMarkSpawns = false;
        };

        _commandService.RegisterCommand(
            ChatCommands.Spawn,
            CommandHandlerSpawn,
            ArgOption.UInt("Teleport to spawn", "spawn number"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.TSpawn,
            CommandHandlerTSpawn,
            ArgOption.UInt("Teleport to t spawn", "t spawn number"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.CtSpawn,
            CommandHandlerCtSpawn,
            ArgOption.UInt("Teleport to ct spawn", "t spawn number"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.BestSpawn,
            CommandHandlerBestSpawn,
            ArgOption.NoArgs("Teleport to the best spawn based on your current position"),
            Permissions.Flags.Spawn);

        _commandService.RegisterCommand(ChatCommands.WorstSpawn,
            CommandHandlerWorstSpawn,
            ArgOption.NoArgs("Teleport to the worst spawn based on your current position"),
            Permissions.Flags.Spawn);

        base.Load(plugin);
    }

    private MapSpawns GetSpawnForCurrentMap()
    {
        lock (_mapSpawnCacheLock)
        {
            if (_mapSpawnsCache is null || _mapSpawnsCache.Map != Server.MapName ||
                _mapSpawnsCache.CounterTerroristSpawns.Count == 0 || _mapSpawnsCache.TerroristSpawns.Count == 0)
            {
                var tSpawns = GetSpawns(CsTeam.Terrorist);
                var ctSpawn = GetSpawns(CsTeam.CounterTerrorist);
                _mapSpawnsCache = new MapSpawns(Server.MapName, tSpawns, ctSpawn);
            }

            return _mapSpawnsCache;
        }
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
            return [];
        }

        var result = new List<Position>();
        var spawns = Utilities.FindAllEntitiesByDesignerName<SpawnPoint>($"info_player_{teamString}").ToList();
        var minPrio = 1;
        foreach (var spawn in spawns)
        {
            if (spawn is { IsValid: true, Enabled: true } && spawn.Priority <= minPrio)
            {
                minPrio = spawn.Priority;
            }
        }

        foreach (var spawn in spawns)
        {
            if (spawn is { IsValid: true, Enabled: true } && spawn.Priority == minPrio)
            {
                result.Add(
                    new Position(
                        spawn.CBodyComponent!.SceneNode!.AbsOrigin,
                        spawn.CBodyComponent.SceneNode.AbsRotation)
                );
            }
        }

        return result;
    }

    private ErrorOr<Success> TeleportToTeamSpawn(CCSPlayerController player, int spawnNumber, CsTeam team = CsTeam.None)
    {
        var mapSpawns = GetSpawnForCurrentMap();

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
            spawns = mapSpawns.TerroristSpawns;
        }
        else if (targetTeam == CsTeam.CounterTerrorist)
        {
            spawns = mapSpawns.CounterTerroristSpawns;
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

    private ErrorOr<Success> CommandHandlerSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        var spawnNumberResult = commandInfo.GetArgUInt();
        if (spawnNumberResult.IsError)
        {
            return spawnNumberResult.FirstError;
        }

        var spawnNumber = (int)spawnNumberResult.Value;

        var teleportResult = TeleportToTeamSpawn(player, spawnNumber);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerCtSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        var spawnNumberResult = commandInfo.GetArgUInt();
        if (spawnNumberResult.IsError)
        {
            return spawnNumberResult.FirstError;
        }

        var spawnNumber = (int)spawnNumberResult.Value;

        var teleportResult = TeleportToTeamSpawn(player, spawnNumber, CsTeam.CounterTerrorist);
        if (teleportResult.IsError)
        {
            return teleportResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerTSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        var spawnNumberResult = commandInfo.GetArgUInt();
        if (spawnNumberResult.IsError)
        {
            return spawnNumberResult.FirstError;
        }

        var spawnNumber = (int)spawnNumberResult.Value;

        var teleportResult = TeleportToTeamSpawn(player, spawnNumber, CsTeam.Terrorist);
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

        var mapSpawns = GetSpawnForCurrentMap();

        List<Position> spawns;
        if (player.Team == CsTeam.Terrorist)
        {
            spawns = mapSpawns.TerroristSpawns;
        }
        else if (player.Team == CsTeam.CounterTerrorist)
        {
            spawns = mapSpawns.CounterTerroristSpawns;
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

    private ErrorOr<Success> CommandHandlerWorstSpawn(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        var mapSpawns = GetSpawnForCurrentMap();

        List<Position> spawns;
        if (player.Team == CsTeam.Terrorist)
        {
            spawns = mapSpawns.TerroristSpawns;
        }
        else if (player.Team == CsTeam.CounterTerrorist)
        {
            spawns = mapSpawns.CounterTerroristSpawns;
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

    #endregion

    private void MarkSpawns()
    {
        var spawns = GetSpawnForCurrentMap();
        _spawnMarkerLaserEntityIds.Clear();
        foreach (var spawn in spawns.TerroristSpawns.Concat(spawns.CounterTerroristSpawns))
        {
            var color = Color.FromArgb(255, 0, 255, 0);
            const float width = 1;
            const float length = 10;
            var height = spawn.Pos.Z;
            const float offset = 1;

            // Top line
            var start = new Vector(spawn.Pos.X - length - offset, spawn.Pos.Y + length, height);
            var end = new Vector(spawn.Pos.X + length + offset, spawn.Pos.Y + length, height);
            var topLine = DrawLaser(start, end, width, color);

            // Bottom line
            start = new Vector(spawn.Pos.X - length - offset, spawn.Pos.Y - length, height);
            end = new Vector(spawn.Pos.X + length + offset, spawn.Pos.Y - length, height);
            var bottomLine = DrawLaser(start, end, width, color);

            // Left line
            start = new Vector(spawn.Pos.X - length, spawn.Pos.Y + length + offset, height);
            end = new Vector(spawn.Pos.X - length, spawn.Pos.Y - length - offset, height);
            var leftLine = DrawLaser(start, end, width, color);

            // Right line
            start = new Vector(spawn.Pos.X + length, spawn.Pos.Y + length + offset, height);
            end = new Vector(spawn.Pos.X + length, spawn.Pos.Y - length - offset, height);
            var rightLine = DrawLaser(start, end, width, color);

            if (topLine is null || bottomLine is null || leftLine is null || rightLine is null)
            {
                _logger.LogError("Failed to mark spawn");
            }
            else
            {
                _spawnMarkerLaserEntityIds.Add((int)topLine.Value);
                _spawnMarkerLaserEntityIds.Add((int)bottomLine.Value);
                _spawnMarkerLaserEntityIds.Add((int)leftLine.Value);
                _spawnMarkerLaserEntityIds.Add((int)rightLine.Value);
            }
        }
    }

    private static uint? DrawLaser(Vector start, Vector end, float width, Color colour)
    {
        var laser = Utilities.CreateEntityByName<CEnvBeam>("env_beam");
        if (laser == null)
        {
            return null;
        }

        laser.Render = colour;
        laser.Width = width;

        laser.Teleport(start, new QAngle(0.0f, 0.0f, 0.0f), new Vector(0.0f, 0.0f, 0.0f));
        laser.EndPos.X = end.X;
        laser.EndPos.Y = end.Y;
        laser.EndPos.Z = end.Z;

        Utilities.SetStateChanged(laser, "CBeam", "m_vecEndPos");

        laser.DispatchSpawn();
        return laser.Index;
    }
}