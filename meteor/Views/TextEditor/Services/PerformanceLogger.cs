using System;
using System.Diagnostics;

namespace meteor.Views.Services;

public class PerformanceLogger(string operationName) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public void Dispose()
    {
        _stopwatch.Stop();
        Debug.WriteLine($"{operationName} took {_stopwatch.ElapsedMilliseconds}ms");
    }
}