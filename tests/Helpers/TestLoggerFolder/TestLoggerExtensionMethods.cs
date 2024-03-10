using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Helpers.TestLoggerFolder;

public static class TestLoggerExtensionMethods
{
    public static IServiceCollection AddTestLogger(this IServiceCollection serviceCollection, ITestOutputHelper outputHelper)
    {
        serviceCollection.AddSingleton(outputHelper);
        serviceCollection.AddSingleton(typeof(ILogger), typeof(XunitLogger<>));
        return serviceCollection;
    }
}