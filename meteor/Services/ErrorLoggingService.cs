using System;
using System.Threading.Tasks;
using meteor.Interfaces;
using Microsoft.Extensions.Logging;

namespace meteor.Views.Services;

public class ErrorLoggingService : IErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;

    public ErrorLoggingService(ILogger<ErrorLoggingService> logger)
    {
        _logger = logger;
    }

    public Task LogErrorAsync(string message, Exception ex)
    {
        _logger.LogError(ex, message);
        return Task.CompletedTask;
    }
}