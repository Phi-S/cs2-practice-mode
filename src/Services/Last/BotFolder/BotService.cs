using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using Cs2PracticeMode.SharedModels;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Cs2PracticeMode.Services.Last.BotFolder;

public partial class BotService : Base
{
    private readonly CommandService _commandService;
    private readonly ILogger<BotService> _logger;
    private readonly MessagingService _messagingService;

    private readonly ConcurrentBag<Timer> _timers = new();

    public BotService(ILogger<BotService> logger, CommandService commandService, MessagingService messagingService)
    {
        _logger = logger;
        _commandService = commandService;
        _messagingService = messagingService;
    }

    /// <summary>
    ///     Dict of a bots Id = userid of bot
    /// </summary>
    private ConcurrentDictionary<int, BotInfo> SpawnedBots { get; } = new();

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

        _commandService.RegisterCommand(ChatCommands.Bot,
            CommandActionBot,
            ArgOption.NoArgs("Place bot on current position"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.TBot,
            CommandActionTBot,
            ArgOption.NoArgs("Place terrorist bot on current position"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.CtBot, CommandActionCtBot,
            ArgOption.NoArgs("Place counter-terrorist bot on current position"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.CrouchBot, CommandActionCrouchBot,
            ArgOption.NoArgs("Place crouching bot on current position"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.BoostOnBot, CommandActionBoost,
            ArgOption.NoArgs("Boost on bot"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.CrouchBoostOnBot, CommandActionCrouchBoost,
            ArgOption.NoArgs("Boost on crouching bot"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.SwapWithBot, CommandActionSwapBot,
            ArgOption.NoArgs("Swap position with closest bot"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.MoveBot, CommandActionMoveBot,
            ArgOption.NoArgs("Move closest bot to current position"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.NoBot, CommandActionNoBot,
            ArgOption.NoArgs("Remove closest bot"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.NoBots, CommandActionNoBots,
            ArgOption.NoArgs("Remove all bots placed by player"),
            Permissions.Flags.Bot);

        _commandService.RegisterCommand(ChatCommands.ClearBots, CommandActionClearBots,
            ArgOption.NoArgs("Remove all bots "),
            Permissions.Flags.ClearBots);

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        foreach (var timer in _timers) timer.Kill();

        SpawnedBots.Clear();
        base.Unload(plugin);
    }

    private void AddTimer(float interval, Action callback, TimerFlags? flags = null)
    {
        var timer = new Timer(interval, callback, flags ?? 0);
        _timers.Add(timer);
    }

    /// <summary>
    ///     Following code is heavily inspired by https://github.com/shobhit-pathak/MatchZy/blob/main/PracticeMode.cs
    /// </summary>
    /// <param name="player">player who added the bot</param>
    /// <param name="team">team(CT/T) the bot will join. If none. The bot will join the opposite team of the player</param>
    /// <param name="crouch">option if the added bot should crouch</param>
    private ErrorOr<Success> AddBot(CCSPlayerController? player, CsTeam team = CsTeam.None, bool crouch = false)
    {
        if (player is null || player.IsValid == false)
        {
            return Errors.PlayerNullOrNotValid();
        }

        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            if (player.TeamNum == (byte)CsTeam.Terrorist)
            {
                team = CsTeam.CounterTerrorist;
            }
            else if (player.TeamNum == (byte)CsTeam.CounterTerrorist)
            {
                team = CsTeam.Terrorist;
            }
        }

        if (team == CsTeam.Terrorist)
        {
            Server.ExecuteCommand("bot_join_team T");
            Server.ExecuteCommand("bot_add_t");
        }
        else if (team == CsTeam.CounterTerrorist)
        {
            Server.ExecuteCommand("bot_join_team CT");
            Server.ExecuteCommand("bot_add_ct");
        }
        else
        {
            return Errors.Fail($"Bots can not be added to team \"{team.ToString()}\"");
        }

        // Adding a small timer so that bot can be added in the world
        // Once bot is added, we teleport it to the requested position
        AddTimer(0.1f, () => SpawnBot(player, team, crouch));
        Server.ExecuteCommand("bot_stop 1");
        Server.ExecuteCommand("bot_freeze 1");
        Server.ExecuteCommand("bot_zombie 1");
        return Result.Success;
    }

    /// <summary>
    ///     get closest bot to the player
    /// </summary>
    /// <param name="player">player called the command</param>
    private BotInfo? GetClosestBotOfPlayer(CCSPlayerController player)
    {
        BotInfo? closestBot = null;
        float smallestDistance = 0;
        foreach (var botInfo in SpawnedBots.Values)
        {
            if (botInfo.Controller.IsValid == false)
            {
                continue;
            }

            if (botInfo.Owner.UserId != player.UserId)
            {
                continue;
            }

            var absolutDistanceResult = AbsolutDistance(botInfo.Owner, botInfo.Controller);
            if (absolutDistanceResult.IsError)
            {
                _logger.LogError(
                    "Failed to calculate absolut distance between bot \"{BotName}\" and player: {BotOwner}",
                    botInfo.Controller.PlayerName, botInfo.Owner.PlayerName);
            }

            var currentDistance = absolutDistanceResult.Value;
            if (currentDistance < smallestDistance || closestBot == null)
            {
                smallestDistance = currentDistance;
                closestBot = botInfo;
            }
        }

        return closestBot;
    }

    /// <summary>
    ///     Calculate difference in coordinates between a player and a bot
    /// </summary>
    /// <param name="player">player</param>
    /// <param name="bot">bot</param>
    /// <returns>absolut distance x+y</returns>
    private static ErrorOr<float> AbsolutDistance(CCSPlayerController player, CCSPlayerController bot)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn is null)
        {
            return Errors.Fail("Player pawn not found");
        }

        var botPawn = bot.PlayerPawn.Value;
        if (botPawn is null)
        {
            return Errors.Fail("Bot pawn not found");
        }

        var playerPos = playerPawn.CBodyComponent!.SceneNode!.AbsOrigin;
        var botPos = botPawn.CBodyComponent!.SceneNode!.AbsOrigin;
        var distanceX = playerPos.X - botPos.X;
        var distanceY = playerPos.Y - botPos.Y;
        var distanceZ = playerPos.Z - botPos.Z;
        if (distanceX < 0)
        {
            distanceX *= -1;
        }

        if (distanceY < 0)
        {
            distanceY *= -1;
        }

        if (distanceZ < 0)
        {
            distanceZ *= -1;
        }

        return distanceX + distanceY + distanceZ;
    }

    private void ElevatePlayer(CCSPlayerController player)
    {
        var playerPawn = player.PlayerPawn.Value;
        if (playerPawn is null || playerPawn.IsValid == false || playerPawn.CBodyComponent?.SceneNode is null)
        {
            _messagingService.MsgToPlayerChat(player, "Failed to elevate. Player pawn not found");
            return;
        }

        playerPawn.Teleport(
            new Vector(
                playerPawn.CBodyComponent.SceneNode.AbsOrigin.X,
                playerPawn.CBodyComponent.SceneNode.AbsOrigin.Y,
                playerPawn.CBodyComponent.SceneNode.AbsOrigin.Z + 80.0f),
            playerPawn.EyeAngles,
            new Vector(0, 0, 0));
    }

    private void SpawnBot(CCSPlayerController player, CsTeam team = CsTeam.None, bool crouch = false)
    {
        if (team != CsTeam.Terrorist && team != CsTeam.CounterTerrorist)
        {
            _logger.LogWarning("Cant spawn bot as {Team}", team.ToString());
            return;
        }

        var playerEntities = Utilities.FindAllEntitiesByDesignerName<CCSPlayerController>("cs_player_controller");
        var botFound = false;
        foreach (var bot in playerEntities)
        {
            if (bot.IsBot == false
                || bot.UserId.HasValue == false
                || bot.TeamNum != (byte)team)
            {
                continue;
            }

            var botUserId = bot.UserId.Value;
            var botAlreadyInUse = SpawnedBots.TryGetValue(botUserId, out _);
            if (botAlreadyInUse)
            {
                continue;
            }

            if (botFound)
            {
                // Kicking the unused bot.
                // We have to do this because bot_add_t/bot_add_ct may add multiple bots but we need only 1, so we kick the remaining unused ones
                Server.ExecuteCommand($"bot_kick {bot.PlayerName}");
                continue;
            }

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn is null || playerPawn.IsValid == false)
            {
                _messagingService.MsgToPlayerChat(player, "Failed to spawn bot. Bot owner pawn not valid");
                return;
            }

            var botPawn = bot.PlayerPawn.Value;
            if (botPawn is null || botPawn.IsValid == false || botPawn.Bot is null)
            {
                _messagingService.MsgToPlayerChat(player, "Failed to spawn bot. Bot pawn not valid");
                return;
            }

            var botOwnerPosition = Position.CopyFrom(playerPawn);
            var spawnedBotInfo = new BotInfo(
                bot,
                botOwnerPosition,
                player,
                crouch,
                DateTime.UtcNow);

            if (SpawnedBots.TryAdd(botUserId, spawnedBotInfo) == false)
            {
                _messagingService.MsgToPlayerChat(player, "Failed to spawn bot. Bot is already in spawned");
                continue;
            }

            botPawn.Teleport(
                botOwnerPosition.Pos,
                botOwnerPosition.Angle,
                new Vector(0, 0, 0));

            if (spawnedBotInfo.Crouch)
            {
                var movementService =
                    new CCSPlayer_MovementServices(botPawn.MovementServices!.Handle);
                AddTimer(0.1f, () => movementService.DuckAmount = 1);
                AddTimer(0.2f, () => botPawn.Bot.IsCrouching = true);
            }

            botFound = true;
        }

        if (botFound == false)
        {
            _messagingService.MsgToPlayerChat(player,
                "Cannot add bots, the team is full! Use .nobots to remove the current bots");
        }
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player.IsValid == false
            || player.IsBot == false
            || player.UserId.HasValue == false
            || player.PlayerPawn.IsValid == false
            || player.PlayerPawn.Value?.Bot == null
            || player.PlayerPawn.Value.MovementServices == null)
        {
            return HookResult.Continue;
        }

        if (SpawnedBots.TryGetValue(player.UserId.Value, out var botInfo) == false)
        {
            return HookResult.Continue;
        }

        // Respawn a bot where it was actually spawned during practice session
        player.PlayerPawn.Value.Teleport(botInfo.Position.Pos, botInfo.Position.Angle, new Vector(0, 0, 0));

        if (botInfo.Crouch)
        {
            player.PlayerPawn.Value.Flags |= (uint)PlayerFlags.FL_DUCKING;
            var movementService = new CCSPlayer_MovementServices(player.PlayerPawn.Value.MovementServices.Handle);
            AddTimer(0.1f, () => movementService.DuckAmount = 1);
            AddTimer(0.2f, () => player.PlayerPawn.Value.Bot.IsCrouching = true);
        }

        return HookResult.Continue;
    }
}