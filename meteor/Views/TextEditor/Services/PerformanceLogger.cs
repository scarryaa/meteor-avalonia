using System;
using System.Diagnostics;

namespace meteor.Views.Services;

public class PerformanceLogger : IDisposable
{
    private readonly string _operationName;
    private readonly Stopwatch _stopwatch;

    public PerformanceLogger(string operationName)
    {
        _operationName = operationName;
        _stopwatch = Stopwatch.StartNew();
    }

    public void Dispose()
    {
        _stopwatch.Stop();
        Debug.WriteLine($"{_operationName} took {_stopwatch.ElapsedMilliseconds}ms");
    }
}