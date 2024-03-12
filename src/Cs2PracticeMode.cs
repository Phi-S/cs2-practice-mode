using System.Reflection;
using CounterStrikeSharp.API.Core;
using Cs2PracticeMode.Services;
using Cs2PracticeMode.Services._0.PluginConfigFolder;
using Cs2PracticeMode.Services._1.AliasStorageFolder;
using Cs2PracticeMode.Services._1.GrenadeStorageFolder;
using Cs2PracticeMode.Services._1.SettingsStorageFolder;
using Cs2PracticeMode.Services._2.CommandFolder;
using Cs2PracticeMode.Services._2.MessagingFolder;
using Cs2PracticeMode.Services._3.BlindTimeFolder;
using Cs2PracticeMode.Services._3.BotFolder;
using Cs2PracticeMode.Services._3.BreakPropsFolder;
using Cs2PracticeMode.Services._3.ChangeMapFolder;
using Cs2PracticeMode.Services._3.ClearGrenadesFolder;
using Cs2PracticeMode.Services._3.CountdownFolder;
using Cs2PracticeMode.Services._3.FakeRconFolder;
using Cs2PracticeMode.Services._3.FlashFolder;
using Cs2PracticeMode.Services._3.GameConfigFolder;
using Cs2PracticeMode.Services._3.GrenadeMenuFolder;
using Cs2PracticeMode.Services._3.LastThrownGrenadeFolder;
using Cs2PracticeMode.Services._3.NoFlashFolder;
using Cs2PracticeMode.Services._3.PlayerDamageFolder;
using Cs2PracticeMode.Services._3.PlayerPermissionsFolder;
using Cs2PracticeMode.Services._3.SettingsFolder;
using Cs2PracticeMode.Services._3.SmokeColorFolder;
using Cs2PracticeMode.Services._3.SmokeFlyTimeFolder;
using Cs2PracticeMode.Services._3.SpawnFolder;
using Cs2PracticeMode.Services._3.SwapTeamsFolder;
using Cs2PracticeMode.Services._3.TimerFolder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cs2PracticeMode;

public class Cs2PracModeServiceCollection : IPluginServiceCollection<Cs2PracticeMode>
{
    public void ConfigureServices(IServiceCollection serviceCollection)
    {
        // 0
        serviceCollection.AddSingleton<PluginConfigService>();

        // 1
        serviceCollection.AddSingleton<SettingsStorageService>();
        serviceCollection.AddSingleton<AliasStorageService>();
        serviceCollection.AddSingleton<GrenadeStorageService>();

        // 3
        serviceCollection.AddSingleton<CommandService>();
        serviceCollection.AddSingleton<MessagingService>();
        
        // 4
        serviceCollection.AddSingleton<SetPlayerPermissionsService>();
        serviceCollection.AddSingleton<GameConfigService>();
        serviceCollection.AddSingleton<FakeRconService>();
        serviceCollection.AddSingleton<SettingsMenuService>();
        serviceCollection.AddSingleton<BlindTimeService>();
        serviceCollection.AddSingleton<PlayerDamageService>();
        serviceCollection.AddSingleton<BreakPropsService>();
        serviceCollection.AddSingleton<LastThrownGrenadeService>();
        serviceCollection.AddSingleton<ClearGrenadesService>();
        serviceCollection.AddSingleton<CountdownService>();
        serviceCollection.AddSingleton<FlashService>();
        serviceCollection.AddSingleton<NoFlashService>();
        serviceCollection.AddSingleton<SmokeColorService>();
        serviceCollection.AddSingleton<SmokeFlyTimeService>();
        serviceCollection.AddSingleton<SpawnService>();
        serviceCollection.AddSingleton<SwapTeamsService>();
        serviceCollection.AddSingleton<TimerService>();
        serviceCollection.AddSingleton<ChangeMapService>();
        serviceCollection.AddSingleton<BotService>();
        serviceCollection.AddSingleton<GrenadeMenuService>();
    }
}

public class Cs2PracticeMode : BasePlugin
{
    private readonly ILogger<Cs2PracticeMode> _logger;
    private readonly IServiceProvider _serviceProvider;

    public Cs2PracticeMode(ILogger<Cs2PracticeMode> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override string ModuleName => Assembly.GetExecutingAssembly().GetName().Name ??
                                         throw new NullReferenceException("AssemblyName");

    public override string ModuleVersion => "1.0";

    public override void Load(bool hotReload)
    {
        var services = GetServicesWithBaseClass<Base>()
            .ToList();

        foreach (var loadOrder in Enum.GetValues<LoadOrder>().Order().ToList())
        {
            var servicesToLoad = services.Where(s => s.LoadOrder == loadOrder)
                .ToList();
            LoadServices(servicesToLoad);
        }

        base.Load(hotReload);
    }

    public override void Unload(bool hotReload)
    {
        var services = GetServicesWithBaseClass<Base>()
            .ToList();

        foreach (var loadOrder in Enum.GetValues<LoadOrder>().OrderDescending().ToList())
        {
            var servicesToUnload = services.Where(s => s.LoadOrder == loadOrder)
                .ToList();
            UnloadServices(servicesToUnload);
        }

        base.Unload(hotReload);
    }

    private void LoadServices(List<Base> services)
    {
        foreach (var service in services)
        {
            if (service.IsLoaded)
            {
                _logger.LogInformation("Cant load service. Service \"{Service}\" is already loaded",
                    service.GetType().Name);
                continue;
            }

            service.Load(this);
        }
    }

    private void UnloadServices(List<Base> services)
    {
        foreach (var service in services)
        {
            if (service.IsLoaded == false)
            {
                _logger.LogInformation("Cant unload service. Service \"{Service}\" is not loaded",
                    service.GetType().Name);
                continue;
            }

            service.Unload(this);
        }
    }

    private List<T> GetServicesWithBaseClass<T>() where T : notnull
    {
        var result = new List<T>();
        var allTypesInAssembly = Assembly
            .GetExecutingAssembly()
            .GetTypes();

        foreach (var type in allTypesInAssembly)
        {
            if (type is { IsClass: true, IsAbstract: false } &&
                type.IsSubclassOf(typeof(T)))
            {
                var service = _serviceProvider.GetRequiredService(type);
                if (service is T serviceForType)
                {
                    result.Add(serviceForType);
                }
            }
        }

        return result;
    }
}