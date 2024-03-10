using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Extensions;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.SharedModels;
using ErrorOr;

namespace Cs2PracticeMode.Services.Last.BotFolder;

public partial class BotService
{
    private ErrorOr<Success> CommandActionBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        var addBotResult = AddBot(player);
        if (addBotResult.IsError)
        {
            return addBotResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionTBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        var addBotResult = AddBot(player, CsTeam.Terrorist);
        if (addBotResult.IsError)
        {
            return addBotResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionCtBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        var addBotResult = AddBot(player, CsTeam.CounterTerrorist);
        if (addBotResult.IsError)
        {
            return addBotResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionCrouchBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        var addBotResult = AddBot(player, CsTeam.None, true);
        if (addBotResult.IsError)
        {
            return addBotResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionBoost(CCSPlayerController player, CommandInfo commandInfo)
    {
        var addBotResult = AddBot(player);
        if (addBotResult.IsError)
        {
            return addBotResult.FirstError;
        }

        AddTimer(0.2f, () => ElevatePlayer(player));
        return Result.Success;
    }


    private ErrorOr<Success> CommandActionCrouchBoost(CCSPlayerController player, CommandInfo commandInfo)
    {
        var addBotResult = AddBot(player, CsTeam.None, true);
        if (addBotResult.IsError)
        {
            return addBotResult.FirstError;
        }

        AddTimer(0.2f, () => ElevatePlayer(player));
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionSwapBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.IsValid == false || player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        var closestBot = GetClosestBotOfPlayer(player);
        if (closestBot == null ||
            closestBot.Controller.IsValid == false ||
            closestBot.Controller.PlayerPawn.IsValid == false ||
            closestBot.Controller.PlayerPawn.Value is null)
        {
            return Errors.Fail("No valid bot found");
        }

        var positionPlayer = Position.CopyFrom(player.PlayerPawn.Value);
        var positionBot = Position.CopyFrom(closestBot.Controller.PlayerPawn.Value);
        player.TeleportToPosition(positionBot);
        closestBot.Controller.TeleportToPosition(positionPlayer);
        _messagingService.MsgToPlayerChat(player,
            $"Swapped your current position with bot {closestBot.Controller.PlayerName}");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionMoveBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.IsValid == false || player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        var lastBotSpawned = SpawnedBots.Values.Where(b => b.Owner == player).MaxBy(b => b.AddedUtc);
        if (lastBotSpawned is null || lastBotSpawned.Controller.IsValid == false)
        {
            return Errors.Fail("Failed to move bot. Could not get last bot spawned");
        }

        if (player.PlayerPawn.IsValid == false || player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Failed to move bot. Could not find player position");
        }

        var currentPosition = Position.CopyFrom(player.PlayerPawn.Value);
        lastBotSpawned.Controller.TeleportToPosition(currentPosition);
        _messagingService.MsgToPlayerChat(player, "Moved bot to your current position.");
        return Result.Success;
    }

    /// <summary>
    ///     Remove closest bot to the player
    /// </summary>
    /// <param name="player">player called the command</param>
    /// <param name="commandInfo"></param>
    private ErrorOr<Success> CommandActionNoBot(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.IsValid == false || player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        var closestBot = GetClosestBotOfPlayer(player);
        if (closestBot?.Controller.UserId is null)
        {
            return Errors.Fail("Closest bot is not valid");
        }

        if (SpawnedBots.TryRemove(closestBot.Controller.UserId.Value, out _) == false)
        {
            return Errors.Fail("Failed to remove closest bot. No valid bot found");
        }

        Server.ExecuteCommand($"bot_kick {closestBot.Controller.PlayerName}");
        return Result.Success;
    }

    private ErrorOr<Success> CommandActionNoBots(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (player.PlayerPawn.IsValid == false || player.PlayerPawn.Value is null)
        {
            return Errors.Fail("Player pawn not valid");
        }

        foreach (var spawnedBotInfo in SpawnedBots.Values.Where(b => b.Owner == player))
        {
            var bot = spawnedBotInfo.Controller;
            if (bot.UserId is null)
            {
                continue;
            }

            SpawnedBots.TryRemove(bot.UserId.Value, out _);
            Server.ExecuteCommand($"bot_kick {bot.PlayerName}");
        }

        return Result.Success;
    }

    private ErrorOr<Success> CommandActionClearBots(CCSPlayerController player, CommandInfo commandInfo)
    {
        SpawnedBots.Clear();
        Server.ExecuteCommand("bot_kick");
        return Result.Success;
    }
}