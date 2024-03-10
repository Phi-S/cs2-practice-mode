using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services.Last.TimerFolder;

public class TimerService : Base
{
    private readonly CommandService _commandService;
    private readonly ILogger<TimerService> _logger;
    private readonly MessagingService _messagingService;
    private readonly ConcurrentDictionary<CCSPlayerController, (DateTime startDate, HtmlPrint print)> _timers = new();

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
            ArgOption.NoArgs("Start or stop timer"),
            Permissions.Flags.Timer);

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _cancellationTokenSource.Cancel();
        foreach (var timer in _timers) _messagingService.HideCenterHtml(timer.Key, timer.Value.print);

        _timers.Clear();

        if (_backgroundTask is not null && _backgroundTask.Status == TaskStatus.Running)
        {
            _logger.LogError("Failed to stop background task in {ManagerName}", nameof(TimerService));
        }

        base.Unload(plugin);
    }

    private ErrorOr<Success> CommandHandlerTimer(CCSPlayerController player, CommandInfo commandInfo)
    {
        var startStopResult = StartStop(player);
        if (startStopResult.IsError)
        {
            return startStopResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> StartStop(CCSPlayerController player)
    {
        if (_timers.TryGetValue(player, out var currentTimer))
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

            if (_timers.TryAdd(player, (startDate, printResult.Value)) == false)
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
        if (_timers.TryRemove(player, out var removedPlayerTimer))
        {
            _messagingService.HideCenterHtml(player, removedPlayerTimer.print);
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
                await Task.Delay(100, _cancellationTokenSource.Token);
                foreach (var player in _timers.Keys)
                {
                    if (player.IsValid == false)
                    {
                        Hide(player);
                        continue;
                    }

                    if (_timers.TryGetValue(player, out var timer) == false)
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