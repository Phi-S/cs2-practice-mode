using System.Reflection;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Events;

namespace Cs2PracticeMode.Extensions;

public static class BasePluginExtension
{
    public static void DeregisterEventHandler<T>(this BasePlugin plugin, BasePlugin.GameEventHandler<T> handler,
        bool post = true) where T : GameEvent
    {
        var name = typeof(T).GetCustomAttribute<EventNameAttribute>()?.Name;
        ArgumentException.ThrowIfNullOrEmpty(name);
        plugin.DeregisterEventHandler(name, handler, post);
    }

    public static void RemoveListener<T>(this BasePlugin plugin, Delegate handler)
    {
        var name = typeof(T).GetCustomAttribute<ListenerNameAttribute>()?.Name;
        ArgumentException.ThrowIfNullOrEmpty(name);
        plugin.RemoveListener(name, handler);
    }
}