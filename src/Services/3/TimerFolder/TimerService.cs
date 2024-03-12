using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services._3.TimerFolder;

public class TimerService : Base
{
    private readonly CommandService _commandService;
    private readonly ILogger<TimerService> _logger;
    private readonly MessagingService _messagingService;

    private ConcurrentDictionary<CCSPlayerController, (DateTime? startDate, HtmlPrint print)> Timer { get; } = new();
    private ConcurrentDictionary<CCSPlayerController, (DateTime startDate, HtmlPrint print)> Timer2 { get; } = new();

    private Task? _backgroundTask;
    private CancellationTokenSource _cancellationTokenSource = new();

    public TimerService(ILogger<TimerService> logger, CommandService commandService,
        MessagingService messagingService)
    {
        _logger = logger;
        _commandService = commandService;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _backgroundTask = BackgroundTask();

        _commandService.RegisterCommand(ChatCommands.Timer,
            CommandHandlerTimer,
            ArgOption.NoArgs(
                "Start timer. If you start moving, the timer will start. If you stop moving, the timer will stop"),
            Permissions.Flags.Timer);

        _commandService.RegisterCommand(ChatCommands.Timer2,
            CommandHandlerTimer2,
            ArgOption.NoArgs("Start or stop timer"),
            Permissions.Flags.Timer);

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _cancellationTokenSource.Cancel();
        foreach (var timer in Timer2)
        {
            _messagingService.HideCenterHtml(timer.Key, timer.Value.print);
        }

        Timer2.Clear();

        if (_backgroundTask is not null && _backgroundTask.Status == TaskStatus.Running)
        {
            _logger.LogError("Failed to stop background task in {ManagerName}", nameof(TimerService));
        }

        base.Unload(plugin);
    }

    private ErrorOr<Success> CommandHandlerTimer(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (Timer2.ContainsKey(player))
        {
            return Errors.Fail("Another timer is already running");
        }

        if (Timer.ContainsKey(player))
        {
            Hide(player);
            _messagingService.MsgToPlayerChat(player, "Timer stopped");
            return Result.Success;
        }

        var printResult = _messagingService.ShowCenterHtml(player, GetTimerText(DateTime.UtcNow));
        if (printResult.IsError)
        {
            return Errors.Fail($"Failed to start timer. {printResult.ErrorMessage()}");
        }

        if (Timer.TryAdd(player, (null, printResult.Value)) == false)
        {
            _logger.LogError(
                "Failed to start timer for player \"{Player}\". TryAdd returned false. This should never happen",
                player.PlayerName);
            _messagingService.HideCenterHtml(player, printResult.Value);
            return Errors.Fail("Failed to start timer");
        }

        _messagingService.MsgToPlayerChat(player,
            "Start moving to start the timer. If you stop moving, the timer will stop");
        return Result.Success;
    }

    private ErrorOr<Success> CommandHandlerTimer2(CCSPlayerController player, CommandInfo commandInfo)
    {
        if (Timer.ContainsKey(player))
        {
            return Errors.Fail("Another timer is already running");
        }

        if (Timer2.TryGetValue(player, out var currentTimer))
        {
            Hide(player);
            _messagingService.MsgToPlayerChat(player,
                $"Timer stopped after {(DateTime.UtcNow - currentTimer.startDate).TotalSeconds:0.00}s");
        }
        else
        {
            var startDate = DateTime.UtcNow;
            var printResult = _messagingService.ShowCenterHtml(player, GetTimerText(startDate));
            if (printResult.IsError)
            {
                return Errors.Fail($"Failed to start timer. {printResult.ErrorMessage()}");
            }

            if (Timer2.TryAdd(player, (startDate, printResult.Value)) == false)
            {
                _logger.LogError(
                    "Failed to start timer for player \"{Player}\". TryAdd returned false. This should never happen",
                    player.PlayerName);
                _messagingService.HideCenterHtml(player, printResult.Value);
                return Errors.Fail("Failed to start timer");
            }

            _messagingService.MsgToPlayerChat(player, "Timer started");
        }

        return Result.Success;
    }

    private void Hide(CCSPlayerController player)
    {
        if (Timer.TryRemove(player, out var removedPlayerTimer))
        {
            _messagingService.HideCenterHtml(player, removedPlayerTimer.print);
        }

        if (Timer2.TryRemove(player, out var removedPlayerTimer2))
        {
            _messagingService.HideCenterHtml(player, removedPlayerTimer2.print);
        }
    }

    private static string GetTimerText(DateTime startDate)
    {
        return $" {ChatColors.Green} Timer {ChatColors.White} {(DateTime.Now - startDate).TotalSeconds:0.00}s";
    }

    private async Task BackgroundTask()
    {
        try
        {
            while (_cancellationTokenSource.IsCancellationRequested == false)
            {
                await Task.Delay(20, _cancellationTokenSource.Token);

                await Server.NextFrameAsync(() =>
                {
                    foreach (var player in Timer.Keys)
                    {
                        if (player.IsValid == false ||
                            player.PlayerPawn.IsValid == false ||
                            player.Team == CsTeam.None ||
                            player.Team == CsTeam.Spectator)
                        {
                            Hide(player);
                            continue;
                        }

                        if (Timer.TryGetValue(player, out var timer) == false)
                        {
                            _logger.LogError(
                                "Player exist as key but TryGetValue returned false. This should never happen");
                            continue;
                        }


                        var playerButtons = player.Buttons;
                        if ((playerButtons & PlayerButtons.Forward) != 0 ||
                            (playerButtons & PlayerButtons.Back) != 0 ||
                            (playerButtons & PlayerButtons.Left) != 0 ||
                            (playerButtons & PlayerButtons.Right) != 0 ||
                            (playerButtons & PlayerButtons.Moveleft) != 0 ||
                            (playerButtons & PlayerButtons.Moveright) != 0)
                        {
                            if (timer.startDate is null)
                            {
                                Timer[player] = (DateTime.UtcNow, timer.print);
                                timer.print.Content = GetTimerText(DateTime.UtcNow);
                            }
                            else
                            {
                                timer.print.Content = GetTimerText(timer.startDate.Value);
                            }
                        }
                        else
                        {
                            if (timer.startDate is not null)
                            {
                                Hide(player);
                                _messagingService.MsgToPlayerChat(player,
                                    $"Timer stopped after {(DateTime.UtcNow - timer.startDate).Value.TotalSeconds:0.00}s");
                            }
                            else
                            {
                                timer.print.Content = GetTimerText(DateTime.UtcNow);
                            }
                        }
                    }
                });

                foreach (var player in Timer2.Keys)
                {
                    if (player.IsValid == false)
                    {
                        Hide(player);
                        continue;
                    }

                    if (Timer2.TryGetValue(player, out var timer) == false)
                    {
                        _logger.LogError(
                            "Player exist as key but TryGetValue returned false. This should never happen");
                        continue;
                    }

                    timer.print.Content = GetTimerText(timer.startDate);
                }
            }
        }
        catch (Exception e)
        {
            if (e is not TaskCanceledException)
            {
                _logger.LogError(e, "Error in background task of {ManagerName}", nameof(TimerService));
            }
        }
    }
}