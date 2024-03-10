using System.Collections.Concurrent;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.CommandFolder;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services.Last.CountdownFolder;

public class CountdownService : Base
{
    private readonly CommandService _commandService;
    private readonly ConcurrentDictionary<CCSPlayerController, (DateTime endDate, HtmlPrint print)> _countdowns = new();
    private readonly ILogger<CountdownService> _logger;
    private readonly MessagingService _messagingService;

    private Task? _backgroundTask;

    private CancellationTokenSource _cancellationTokenSource = new();

    public CountdownService(ILogger<CountdownService> logger, CommandService commandService,
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
        _commandService.RegisterCommand(ChatCommands.Countdown,
            CommandHandlerCountdown,
            ArgOption.SingleDouble("Starts a countdown", "duration"),
            Permissions.Flags.Countdown
        );

        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _cancellationTokenSource.Cancel();
        _countdowns.Clear();

        if (_backgroundTask is not null && _backgroundTask.Status == TaskStatus.Running)
        {
            _logger.LogError("Failed to stop background task in {ManagerName}", nameof(CountdownService));
        }

        base.Unload(plugin);
    }

    private ErrorOr<Success> CommandHandlerCountdown(CCSPlayerController player, CommandInfo commandInfo)
    {
        var argDouble = commandInfo.GetArgDouble();
        if (argDouble.IsError)
        {
            return argDouble.FirstError;
        }

        var countdownTime = argDouble.Value;

        var startCountdownResult = StartCountdown(player, TimeSpan.FromSeconds(countdownTime));
        if (startCountdownResult.IsError)
        {
            return startCountdownResult.FirstError;
        }

        return Result.Success;
    }

    private ErrorOr<Success> StartCountdown(CCSPlayerController player, TimeSpan countdownTime)
    {
        if (countdownTime.TotalMinutes > 5)
        {
            return Errors.Fail("Cant start countdowns that are longer then 5 Minutes");
        }

        if (_countdowns.TryGetValue(player, out _))
        {
            return Errors.Fail("Another countdown is already running");
        }

        var endDate = DateTime.Now.Add(countdownTime);
        var printResult = _messagingService.ShowCenterHtml(player, GetCountdownText(endDate));
        if (printResult.IsError)
        {
            return Errors.Fail($"Failed to print countdown html. {printResult.ErrorMessage()}");
        }

        if (_countdowns.TryAdd(player, (endDate, printResult.Value)) == false)
        {
            _logger.LogError(
                "Failed to start countdown for player \"{Player}\". TryAdd returned false. " +
                "This should never happen",
                player.PlayerName);
            _messagingService.HideCenterHtml(player, printResult.Value);
            return Errors.Fail("Failed to start new countdown");
        }

        _messagingService.MsgToPlayerChat(player, "Countdown started");
        return Result.Success;
    }

    private void Hide(CCSPlayerController player)
    {
        if (_countdowns.TryRemove(player, out var countdown))
        {
            var hidePrintResult = _messagingService.HideCenterHtml(player, countdown.print);
            if (hidePrintResult.IsError)
            {
                _logger.LogError("Failed to hide countdown html for player \"{Player}\"", player.PlayerName);
            }
            else
            {
                _messagingService.MsgToPlayerChat(player, "Countdown finished");
            }
        }
    }

    private string GetCountdownText(DateTime endDate)
    {
        return $"Countdown <font color=\"green\">{(endDate - DateTime.UtcNow).TotalSeconds:0.00}s</font> ";
    }

    private async Task BackgroundTask()
    {
        try
        {
            while (_cancellationTokenSource.IsCancellationRequested == false)
            {
                await Task.Delay(100, _cancellationTokenSource.Token);
                foreach (var player in _countdowns.Keys)
                {
                    if (player.IsValid == false)
                    {
                        Hide(player);
                        continue;
                    }

                    if (_countdowns.TryGetValue(player, out var countdownForPlayer) == false)
                    {
                        _logger.LogError(
                            "Player exist as key but TryGetValue returned false. This should never happen");
                        continue;
                    }

                    countdownForPlayer.print.Content = GetCountdownText(countdownForPlayer.endDate);

                    if (DateTime.UtcNow >= countdownForPlayer.endDate)
                    {
                        Hide(player);
                    }
                }
            }
        }
        catch (Exception e)
        {
            if (e is not TaskCanceledException)
            {
                _logger.LogError(e, "Error in background task of {ManagerName}", nameof(CountdownService));
            }
        }
    }
}