using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Services.Second.MessagingFolder;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode.Services.Last.SmokeFlyTimeFolder;

public class SmokeFlyTimeService : Base
{
    private readonly ConcurrentDictionary<int, DateTime> _lastThrownSmoke = new();
    private readonly ILogger<SmokeFlyTimeService> _logger;
    private readonly MessagingService _messagingService;

    public SmokeFlyTimeService(ILogger<SmokeFlyTimeService> logger, MessagingService messagingService)
    {
        _logger = logger;
        _messagingService = messagingService;
    }

    public override void Load(BasePlugin plugin)
    {
        plugin.RegisterListener<Listeners.OnEntitySpawned>(entity => OnEntitySpawned(entity));
        plugin.RegisterEventHandler<EventSmokegrenadeDetonate>(OnSmokeDetonate, HookMode.Post);
        base.Load(plugin);
    }

    public override void Unload(BasePlugin plugin)
    {
        _lastThrownSmoke.Clear();
        base.Unload(plugin);
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        if (entity.DesignerName.Equals(DesignerNames.ProjectileSmoke) == false)
        {
            return;
        }

        if (_lastThrownSmoke.TryAdd((int)entity.Index, DateTime.UtcNow) == false)
        {
            _logger.LogError(
                "Failed to add smoke to last thrown smokes. Smoke already added. This should never happen");
        }
    }

    private HookResult OnSmokeDetonate(EventSmokegrenadeDetonate @event, GameEventInfo info)
    {
        if (_lastThrownSmoke.TryGetValue(@event.Entityid, out var result) == false)
        {
            _logger.LogError("Failed to get detonated smoke from last thrown smoke");
            return HookResult.Continue;
        }

        var projectile = Utilities.GetEntityFromIndex<CSmokeGrenadeProjectile>(@event.Entityid);
        _messagingService.MsgToAll(
            $"Smoke thrown by {ChatColors.Blue}{@event.Userid.PlayerName}{ChatColors.White}" +
            $" took {ChatColors.Green}{(DateTime.UtcNow - result).TotalSeconds.ToString("0.00")}{ChatColors.White}s" +
            $" and {ChatColors.Green}{projectile.Bounces}{ChatColors.White} bounces to detonate");

        return HookResult.Continue;
    }
}