using System;
using Microsoft.Extensions.Logging;

namespace meteor.Services;

public class ConsoleLogger(string categoryName) : ILogger
{
    public IDisposable BeginScope<TState>(TState state)
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        Console.WriteLine(
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {categoryName}: {formatter(state, exception)}");
        if (exception != null) Console.WriteLine($"Exception: {exception}");
    }
}