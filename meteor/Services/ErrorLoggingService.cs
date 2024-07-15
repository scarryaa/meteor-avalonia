using System;
using System.Threading.Tasks;
using meteor.Interfaces;
using Microsoft.Extensions.Logging;

namespace meteor.Services;

public class ErrorLoggingService(ILogger<ErrorLoggingService> logger) : IErrorLoggingService
{
    public Task LogErrorAsync(string message, Exception ex)
    {
        logger.LogError(ex, message);
        return Task.CompletedTask;
    }
}