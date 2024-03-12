using CounterStrikeSharp.API.Core;

namespace Cs2PracticeMode.Services;

public abstract class Base
{
    protected Base(LoadOrder loadOrder = LoadOrder.Normal)
    {
        LoadOrder = loadOrder;
    }

    public LoadOrder LoadOrder { get; }
    public bool IsLoaded { get; private set; }

    public virtual void Load(BasePlugin plugin)
    {
        IsLoaded = true;
    }

    public virtual void Unload(BasePlugin plugin)
    {
        IsLoaded = false;
    }
}