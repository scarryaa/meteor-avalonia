using System;
using System.Threading.Tasks;

namespace meteor.Interfaces;

public interface IErrorLoggingService
{
    Task LogErrorAsync(string message, Exception ex);
}