using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Helpers.TestLoggerFolder;

public class XunitLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper _output;
    
    public XunitLogger(ITestOutputHelper output)
    {
        _output = output;
    }


#pragma warning disable CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.
    public IDisposable BeginScope<TState>(TState state) => null!;
#pragma warning restore CS8633 // Nullability in constraints for type parameter doesn't match the constraints for type parameter in implicitly implemented interface method'.


    public bool IsEnabled(LogLevel logLevel) => true;


    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception, string> formatter)

    {
        _output.WriteLine(formatter(state, exception!));
    }
}